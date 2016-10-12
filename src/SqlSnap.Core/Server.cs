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
                new Operation(_instanceName, databaseName, OperationMode.Backup, metadataStream, false, snapshotAction)
                    .ExecuteAsync();

            Log.Information("Backed up {databaseName} successfully");
        }
    }
}