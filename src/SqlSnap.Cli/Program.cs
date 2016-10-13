using System;
using System.Diagnostics;
using System.IO;
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
                .ParseArguments<BackupOptions, RestoreOptions>(args)
                .MapResult(
                    (BackupOptions opts) => Backup(opts),
                    (RestoreOptions opts) => Restore(opts),
                    errs => 1);
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
            var process = Process.Start(new ProcessStartInfo("cmd.exe", $"/C \"{command}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                Log.Information(line);
            }
            process.WaitForExit();
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

        private static int Restore(RestoreOptions options)
        {
            ConfigureLogging(options.Verbose);

            try
            {
                using (var stream = File.Open(options.MetadataPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var snapshotCommand = string.IsNullOrEmpty(options.SnapshotCommand)
                        ? (Action) null
                        : () => RunSnapshotCommand(options.SnapshotCommand);

                    var server = new Server(options.InstanceName);
                    server.RestoreAsync(options.Database, stream, snapshotCommand, options.NoRecovery)
                        .Wait();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to complete the restore");
                return -1;
            }

            return 0;
        }
    }
}