using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private static Database[] OpenDatabases(string metadataPath, IList<string> databaseNames, FileMode mode, FileAccess access)
        {
            return databaseNames.Select(databaseName => new Database
            {
                Name = databaseName,
                MetadataStream =
                    File.Open(Path.Combine(metadataPath, $"{databaseName}.metadata"), mode, access, FileShare.None)
            }).ToArray();
        }

        private static int Backup(BackupOptions options)
        {
            ConfigureLogging(options.Verbose);

            Database[] databases = null;

            try
            {
                databases = OpenDatabases(options.MetadataPath, options.Database, FileMode.Create, FileAccess.Write);
                var server = new Server(options.InstanceName);
                server.BackupAsync(databases, () => RunSnapshotCommand(options.SnapshotCommand), options.Timeout)
                    .Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to complete the backup");
                return -1;
            }
            finally
            {
                if (databases != null)
                    foreach (var database in databases)
                        database.MetadataStream.Dispose();
            }

            return 0;
        }

        private static int Restore(RestoreOptions options)
        {
            ConfigureLogging(options.Verbose);

            Database[] databases = null;

            try
            {
                databases = OpenDatabases(options.MetadataPath, options.Database, FileMode.Open, FileAccess.Read);
                var snapshotCommand = string.IsNullOrEmpty(options.SnapshotCommand)
                    ? (Action) null
                    : () => RunSnapshotCommand(options.SnapshotCommand);

                var server = new Server(options.InstanceName);
                server.RestoreAsync(databases, snapshotCommand, options.NoRecovery, options.Timeout)
                    .Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to complete the restore");
                return -1;
            }
            finally
            {
                if (databases != null)
                    foreach (var database in databases)
                        database.MetadataStream.Dispose();
            }

            return 0;
        }
    }
}