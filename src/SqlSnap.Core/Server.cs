using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace SqlSnap.Core
{
    public class Server
    {
        private readonly string _instanceName;

        public Server(string instanceName)
        {
            _instanceName = instanceName;
        }

        public Server() : this("")
        {
        }

        public async Task BackupAsync(string databaseName, Stream metadataStream, Action snapshotAction)
        {
            Log.Information("Preparing to backup {databaseName}", databaseName);

            await
                new Operation(_instanceName, databaseName, OperationMode.Backup, metadataStream, snapshotAction, false)
                    .ExecuteAsync();

            Log.Information("Backed up {databaseName} successfully", databaseName);
        }

        public async Task RestoreAsync(string databaseName, Stream metadataStream, Action snapshotMountAction,
            bool noRecovery)
        {
            Log.Information("Preparing to restore {databaseName}", databaseName);

            await
                new Operation(_instanceName, databaseName, OperationMode.Restore, metadataStream, snapshotMountAction, noRecovery)
                    .ExecuteAsync();

            Log.Information("Restored {databaseName} successfully", databaseName);
        }
    }
}