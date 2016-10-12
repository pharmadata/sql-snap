using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;
using SqlSnap.Core.Vdi;

namespace SqlSnap.Core
{
    internal class Operation
    {
        private readonly string _instanceName;
        private readonly string _databaseName;
        private readonly Stream _metadataStream;
        private readonly OperationMode _mode;
        private readonly bool _noRecovery;
        private readonly Action _snapshotAction;

        private readonly Dictionary<VDCommandCode, Func<VDC_Command, int>> _commandHandlers;

        public Operation(string instanceName, string databaseName, OperationMode mode, Stream metadataStream, bool noRecovery, Action snapshotAction)
        {
            _instanceName = instanceName;
            _mode = mode;
            _metadataStream = metadataStream;
            _noRecovery = noRecovery;
            _snapshotAction = snapshotAction;
            _databaseName = databaseName;

            _commandHandlers = new Dictionary<VDCommandCode, Func<VDC_Command, int>>
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
            var builder = new SqlConnectionStringBuilder()
            {
                DataSource = string.IsNullOrEmpty(_instanceName) ? "." : $".\\{_instanceName}",
                IntegratedSecurity = true
            };
            var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return connection;
        }

        private string GetSqlCommand(string virtualDeviceName)
        {
            var sqlOperation = _mode == OperationMode.Backup ? "BACKUP" : "RESTORE";
            var direction = _mode == OperationMode.Backup ? "TO" : "FROM";

            var options = _mode == OperationMode.Backup || !_noRecovery
                ? ""
                : ", NORECOVERY";

            return
                $"{sqlOperation} DATABASE [{_databaseName}] {direction} VIRTUAL_DEVICE='{virtualDeviceName}' WITH SNAPSHOT{options}";
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
                    if ((uint)ex.ErrorCode == 0x80770003) // timeout
                        continue;
                    throw;
                }
            }
            return virtualDeviceSet.OpenDevice(virtualDeviceName);
        }

        private int HandleRead(VDC_Command command)
        {
            var buffer = new byte[command.Size];
            _metadataStream.Read(buffer, 0, command.Size);
            Marshal.Copy(buffer, 0, command.Buffer, command.Size);
            return 0;
        }

        private int HandleWrite(VDC_Command command)
        {
            var buffer = new byte[command.Size];
            Marshal.Copy(command.Buffer, buffer, 0, command.Size);
            _metadataStream.Write(buffer, 0, command.Size);
            return 0;
        }

        private int HandlePrepareToFreeze(VDC_Command command)
        {
            Log.Debug("Database is freezing in preparation for snapshot");
            return 0;
        }

        private int HandleClearError(VDC_Command command)
        {
            return 0;
        }

        private int HandleSnapshot(VDC_Command command)
        {
            Log.Information("Beginning snapshot");
            _snapshotAction();
            Log.Information("Snapshot completed successfully");
            return 0;
        }

        private int HandleFlush(VDC_Command command)
        {
            _metadataStream.Flush();
            return 0;
        }

        private void HandleCommands(IClientVirtualDevice virtualDevice)
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
                    _metadataStream.Close();
                    break;
                }

                var command = (VDC_Command)Marshal.PtrToStructure(commandPointer, typeof(VDC_Command));
                Func<VDC_Command, int> handler;
                if (!_commandHandlers.TryGetValue(command.CommandCode, out handler))
                {
                    virtualDevice.CompleteCommand(commandPointer, 50, command.Size, 0); // 50 = unsupported
                    continue;
                }

                var completionCode = handler(command);
                virtualDevice.CompleteCommand(commandPointer, completionCode, command.Size, 0);
            }
        }

        public async Task ExecuteAsync()
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                var virtualDeviceName = $"sqlsnap\\{Guid.NewGuid()}";

                command.CommandText = GetSqlCommand(virtualDeviceName);

                var config = new VDConfig
                {
                    DeviceCount = 1,
                    Features = VDFeature.SnapshotPrepare
                };

                Log.Debug("Creating virtual device {virtualDeviceName}", virtualDeviceName);

                var virtualDeviceSet = (IClientVirtualDeviceSet2)new ClientVirtualDeviceSet2();
                virtualDeviceSet.CreateEx(_instanceName, virtualDeviceName, ref config);

                try
                {
                    Log.Debug("Executing SQL query: {commandText}", command.CommandText);
                    var queryTask = command.ExecuteNonQueryAsync();

                    Log.Debug("Waiting for virtual device to be ready");
                    var virtualDevice = WaitForDevice(virtualDeviceSet, virtualDeviceName);

                    Log.Debug("Receiving commands from virtual device");
                    HandleCommands(virtualDevice);

                    Log.Debug("Waiting for query to finish");
                    await queryTask;
                }
                finally
                {
                    virtualDeviceSet.Close();
                }
            }
        }
    }
}