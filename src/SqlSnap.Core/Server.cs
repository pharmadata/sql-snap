using System;
using System.Linq;
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

        public async Task BackupAsync(Database[] databases, Action snapshotAction, int timeout)
        {
            Log.Information("Preparing to backup {databases}", string.Join(", ", databases.Select(d => d.Name)));

            await
                new Operation(_instanceName, databases, OperationMode.Backup, snapshotAction, false, timeout)
                    .ExecuteAsync();

            Log.Information("Backed up {databases} successfully", string.Join(", ", databases.Select(d => d.Name)));
        }

        public async Task RestoreAsync(Database[] databases, Action snapshotMountAction, bool noRecovery, int timeout)
        {
            Log.Information("Preparing to restore {databases}", string.Join(", ", databases.Select(d => d.Name)));

            await
                new Operation(_instanceName, databases, OperationMode.Restore, snapshotMountAction, noRecovery, timeout)
                    .ExecuteAsync();

            Log.Information("Restored {databases} successfully", string.Join(", ", databases.Select(d => d.Name)));
        }
    }
}