using CommandLine;

namespace SqlSnap.Cli
{
    [Verb("backup", HelpText = "Execute a SQL Server snapshot backup")]
    public class BackupOptions
    {
        [Option('i', "instanceName", HelpText = "Name of SQL Server instance for which to connect (optional)")]
        public string InstanceName { get; set; }

        [Option('m', "metadata", Required = true, HelpText = "Path to file to store backup metadata")]
        public string MetadataPath { get; set; }

        [Option('d', "database", Required = true, HelpText = "Database to backup")]
        public string Database { get; set; }

        [Option('c', "command", Required = true, HelpText = "Command to execute that performs the snapshot")]
        public string SnapshotCommand { get; set; }

        [Option('v', "verbose", HelpText = "Include verbose logging information")]
        public bool Verbose { get; set; }

        [Option('t', "timeout", HelpText = "Timeout for backup operation (in seconds)", Default = 600)]
        public int Timeout { get; set; }
    }
}