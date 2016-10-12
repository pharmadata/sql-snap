using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Serilog;
using Serilog.Events;
using SqlSnap.Core;

namespace SqlSnap.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default
                .ParseArguments<BackupOptions>(args)
                .MapResult(Backup, errs => 1);
        }

        private static void ConfigureLogging(bool verbose)
        {
            var config = new LoggerConfiguration()
                .MinimumLevel.Is(verbose ? LogEventLevel.Verbose : LogEventLevel.Information)
                .WriteTo.ColoredConsole();
            Log.Logger = config.CreateLogger();
        }

        private static void RunSnapshotCommand(string command)
        {
            var process = Process.Start(command);
            if (process.ExitCode != 0)
                throw new IOException($"Unable to complete snapshot due to non-zero exit code {process.ExitCode}");
        }

        private static int Backup(BackupOptions options)
        {
            ConfigureLogging(options.Verbose);

            try
            {
                using (var stream = File.Open(options.MetadataPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var server = new Server(options.InstanceName);
                    server.BackupAsync(options.Database, stream, () => RunSnapshotCommand(options.SnapshotCommand))
                        .Wait();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to complete the backup");
                return -1;
            }

            return 0;
        }
    }
}