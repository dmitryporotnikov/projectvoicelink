using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectVoiceLink
{
    public record MaintenanceResult(int VoicesDeleted, int FilesDeleted, int OrphanFilesDeleted, long BytesFreed, TimeSpan Duration);

    public record StorageStats(
        long TotalVoices, 
        long TotalUsers, 
        long TotalLogs, 
        long DatabaseSizeBytes, 
        int AudioFilesCount, 
        long AudioDirectorySizeBytes);

    public class MaintenanceService
    {
        private readonly DatabaseService _databaseService;

        public MaintenanceService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<MaintenanceResult> PurgeOlderThanDaysAsync(int days, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, days));

            int filesDeleted = 0;
            int orphanFilesDeleted = 0;
            long bytesFreed = 0;

            // 1. Get audio files to delete from old voices
            var oldAudioFiles = await _databaseService.GetVoicePathsOlderThanAsync(cutoff, cancellationToken);
            foreach (var path in oldAudioFiles)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var fi = new FileInfo(path);
                        bytesFreed += fi.Length;
                        File.Delete(path);
                        filesDeleted++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Maintenance] Failed to delete file {path}: {ex.Message}");
                }
            }

            // 2. Delete database records
            int recordsDeleted = await _databaseService.DeleteRecordsOlderThanAsync(cutoff, cancellationToken);

            // 3. Clean up orphaned files in audio directory
            var storagePath = Configuration.AudioStoragePath;
            if (Directory.Exists(storagePath))
            {
                var knownPaths = await _databaseService.GetAllKnownAudioPathsAsync(cancellationToken);
                var directoryFiles = Directory.GetFiles(storagePath, "*.ogg", SearchOption.AllDirectories);

                foreach (var file in directoryFiles)
                {
                    var full = Path.GetFullPath(file);
                    if (!knownPaths.Contains(full))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            bytesFreed += fi.Length;
                            File.Delete(file);
                            orphanFilesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Maintenance] Failed to delete orphan file {file}: {ex.Message}");
                        }
                    }
                }
            }

            // 4. Reclaim SQLite space
            try
            {
                await _databaseService.VacuumAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Maintenance] Vacuum error: {ex.Message}");
            }

            sw.Stop();
            return new MaintenanceResult(recordsDeleted, filesDeleted, orphanFilesDeleted, bytesFreed, sw.Elapsed);
        }

        public async Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
        {
            var dbStats = await _databaseService.GetStatsAsync(cancellationToken);

            int audioCount = 0;
            long audioBytes = 0;

            var storagePath = Configuration.AudioStoragePath;
            if (Directory.Exists(storagePath))
            {
                var files = Directory.GetFiles(storagePath, "*.ogg", SearchOption.AllDirectories);
                audioCount = files.Length;
                foreach (var file in files)
                {
                    try
                    {
                        audioBytes += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // Ignore files in-use
                    }
                }
            }

            return new StorageStats(
                dbStats.TotalVoices,
                dbStats.TotalUsers,
                dbStats.TotalLogs,
                dbStats.DatabaseSizeBytes,
                audioCount,
                audioBytes
            );
        }

        public void StartScheduledMaintenance(CancellationToken cancellationToken)
        {
            var intervalHours = Configuration.MaintenanceIntervalHours;
            if (intervalHours <= 0) return;

            Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));
                Console.WriteLine($"[Maintenance] Scheduled background cleaner initialized. Interval: {intervalHours}h, Retention: {Configuration.RetentionDays} days.");

                while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        Console.WriteLine("[Maintenance] Running scheduled purge...");
                        var result = await PurgeOlderThanDaysAsync(Configuration.RetentionDays, cancellationToken);
                        Console.WriteLine($"[Maintenance] Purge completed: {result.VoicesDeleted} records deleted, {result.FilesDeleted} audio files removed, {result.OrphanFilesDeleted} orphans removed, {Utilities.FormatBytes(result.BytesFreed)} freed in {result.Duration.TotalSeconds:F2}s.");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Maintenance] Error during scheduled maintenance: {ex.Message}");
                    }
                }
            }, cancellationToken);
        }
    }
}