using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Serilog;
using SqlSnap.Core.Vdi;

namespace SqlSnap.Core
{
    internal class Operation
    {
        private readonly Dictionary<VDCommandCode, Func<Database, VDC_Command, int>> _commandHandlers;
        private readonly Database[] _databases;
        private readonly string _instanceName;
        private readonly OperationMode _mode;
        private readonly bool _noRecovery;
        private readonly Action _snapshotAction;
        private readonly int _timeout;

        private ManualResetEvent _snapshotResetEvent;
        private AsyncCountdownEvent _snapshotCountdown;

        public Operation(string instanceName, Database[] databases, OperationMode mode,
            Action snapshotAction, bool noRecovery, int timeout)
        {
            _instanceName = instanceName;
            _mode = mode;
            _databases = databases;
            _noRecovery = noRecovery;
            _timeout = timeout;
            _snapshotAction = snapshotAction;

            _commandHandlers = new Dictionary<VDCommandCode, Func<Database, VDC_Command, int>>
            {
                {VDCommandCode.Read, HandleRead},
                {VDCommandCode.Write, HandleWrite},
                {VDCommandCode.ClearError, HandleClearError},
                {VDCommandCode.Flush, HandleFlush},
                {VDCommandCode.MountSnapshot, HandleSnapshot},
                {VDCommandCode.Snapshot, HandleSnapshot},
                {VDCommandCode.PrepareToFreeze, HandlePrepareToFreeze}
            };
        }

        private SqlConnection GetConnection()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrEmpty(_instanceName) ? "." : $".\\{_instanceName}",
                IntegratedSecurity = true
            };
            var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return connection;
        }

        private string GetSqlCommand(string virtualDeviceName, string databaseName)
        {
            var sqlOperation = _mode == OperationMode.Backup ? "BACKUP" : "RESTORE";
            var direction = _mode == OperationMode.Backup ? "TO" : "FROM";

            var options = _mode == OperationMode.Backup || !_noRecovery
                ? ""
                : ", NORECOVERY";

            return
                $"{sqlOperation} DATABASE [{databaseName}] {direction} VIRTUAL_DEVICE='{virtualDeviceName}' WITH SNAPSHOT{options}";
        }

        private IClientVirtualDevice WaitForDevice(IClientVirtualDeviceSet2 virtualDeviceSet, string virtualDeviceName)
        {
            var config = new VDConfig();
            while (true)
            {
                try
                {
                    virtualDeviceSet.GetConfiguration(1000, ref config);
                    break;
                }
                catch (COMException ex)
                {
                    if ((uint) ex.ErrorCode == 0x80770003) // timeout
                        continue;
                    throw;
                }
            }
            return virtualDeviceSet.OpenDevice(virtualDeviceName);
        }

        private int HandleRead(Database database, VDC_Command command)
        {
            var buffer = new byte[command.Size];
            database.MetadataStream.Read(buffer, 0, command.Size);
            Marshal.Copy(buffer, 0, command.Buffer, command.Size);
            return 0;
        }

        private int HandleWrite(Database database, VDC_Command command)
        {
            var buffer = new byte[command.Size];
            Marshal.Copy(command.Buffer, buffer, 0, command.Size);
            database.MetadataStream.Write(buffer, 0, command.Size);
            return 0;
        }

        private int HandlePrepareToFreeze(Database database, VDC_Command command)
        {
            Log.Debug("Database is freezing in preparation for snapshot");
            return 0;
        }

        private int HandleClearError(Database database, VDC_Command command)
        {
            return 0;
        }

        private int HandleSnapshot(Database database, VDC_Command command)
        {
            _snapshotCountdown.Signal();
            _snapshotResetEvent.WaitOne();
            return 0;
        }

        private int HandleFlush(Database database, VDC_Command command)
        {
            database.MetadataStream.Flush();
            return 0;
        }

        private void HandleCommands(Database database, IClientVirtualDevice virtualDevice)
        {
            while (true)
            {
                IntPtr commandPointer;
                try
                {
                    commandPointer = virtualDevice.GetCommand(-1);
                }
                catch (COMException ex)
                {
                    // throw an exception unless we are told that the virtual device is closed
                    if ((uint) ex.ErrorCode != 0x8077000E) throw;

                    Log.Information("Metadata has been written successfully");
                    database.MetadataStream.Close();
                    break;
                }

                var command = (VDC_Command) Marshal.PtrToStructure(commandPointer, typeof (VDC_Command));
                Log.Debug("Received command {command} for database {database}", command.CommandCode, database.Name);
                Func<Database, VDC_Command, int> handler;
                if (!_commandHandlers.TryGetValue(command.CommandCode, out handler))
                {
                    virtualDevice.CompleteCommand(commandPointer, 50, command.Size, 0); // 50 = unsupported
                    continue;
                }

                var completionCode = handler(database, command);
                virtualDevice.CompleteCommand(commandPointer, completionCode, command.Size, 0);
            }
        }

        public async Task ExecuteAsync()
        {
            _snapshotResetEvent = new ManualResetEvent(false);
            _snapshotCountdown = new AsyncCountdownEvent(_databases.Length);
            var tasks = _databases.Select(ExecuteAsync).ToArray();

            await _snapshotCountdown.WaitAsync();

            Log.Information("Beginning snapshot");
            _snapshotAction?.Invoke();
            Log.Information("Snapshot completed successfully");

            _snapshotResetEvent.Set();

            await Task.WhenAll(tasks);
        }

        private async Task ExecuteAsync(Database database)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                var virtualDeviceName = $"sqlsnap-{Guid.NewGuid()}";

                command.CommandText = GetSqlCommand(virtualDeviceName, database.Name);
                command.CommandTimeout = _timeout;

                var config = new VDConfig
                {
                    DeviceCount = 1,
                    Features = VDFeature.SnapshotPrepare
                };

                Log.Debug("Creating virtual device {virtualDeviceName}", virtualDeviceName);

                var virtualDeviceSet = (IClientVirtualDeviceSet2) new ClientVirtualDeviceSet2();
                virtualDeviceSet.CreateEx(_instanceName, virtualDeviceName, ref config);

                try
                {
                    Log.Debug("Executing SQL query: {commandText}", command.CommandText);
                    var queryTask = command.ExecuteNonQueryAsync();

                    await Task.Run(() =>
                    {
                        Log.Debug("Waiting for virtual device to be ready");
                        var virtualDevice = WaitForDevice(virtualDeviceSet, virtualDeviceName);

                        Log.Debug("Receiving commands from virtual device");
                        HandleCommands(database, virtualDevice);
                    });

                    Log.Debug("Waiting for query to finish");
                    await queryTask;
                }
                finally
                {
                    try
                    {
                        virtualDeviceSet.Close();
                    }
                    catch
                    {
                        // ignore errors when closing
                    }
                }
            }
        }
    }
}