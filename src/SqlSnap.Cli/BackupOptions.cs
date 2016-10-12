using CommandLine;

namespace SqlSnap.Cli
{
    [Verb("backup", HelpText = "Execute a SQL Server snapshot backup")]
    public class BackupOptions
    {
        [Option("instanceName", HelpText = "Name of SQL Server instance for which to connect")]
        public string InstanceName { get; set; }

        [Option("metadata", HelpText = "Path to file to store backup metadata")]
        public string MetadataPath { get; set; }

        [Option("database", HelpText = "Database to backup")]
        public string Database { get; set; }

        [Option("command", HelpText = "Command to execute that performs the snapshot")]
        public string SnapshotCommand { get; set; }

        [Option("verbose", HelpText = "Include verbose logging information")]
        public bool Verbose { get; set; }
    }
}