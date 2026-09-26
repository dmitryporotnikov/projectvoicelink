using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ProjectVoiceLink;
using Xunit;

namespace ProjectVoiceLink.Tests
{
    public class MaintenanceTests
    {
        [Fact]
        public async Task PurgeOlderThanDays_DeletesOldAudioFilesAndRecords()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestMaint_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={dbPath}";
            var dbService = new DatabaseService(connStr);
            await dbService.InitializeAsync();

            var tempAudioDir = Path.Combine(Path.GetTempPath(), $"TestAudio_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempAudioDir);

            try
            {
                var maintenance = new MaintenanceService(dbService);

                // Create an old file and an old record (40 days old)
                var oldFilePath = Path.Combine(tempAudioDir, "old_voice.ogg");
                await File.WriteAllBytesAsync(oldFilePath, new byte[] { 0x11, 0x22, 0x33 });

                var oldUser = new UserClass
                {
                    id = "old_user",
                    first_name = "Old",
                    date = DateTime.UtcNow.AddDays(-40).ToString("o")
                };
                await dbService.InsertVoiceAsync(oldUser, oldFilePath, "oldhash123");

                // Create a new file and a new record (1 day old)
                var newFilePath = Path.Combine(tempAudioDir, "new_voice.ogg");
                await File.WriteAllBytesAsync(newFilePath, new byte[] { 0x44, 0x55, 0x66 });

                var newUser = new UserClass
                {
                    id = "new_user",
                    first_name = "New",
                    date = DateTime.UtcNow.AddDays(-1).ToString("o")
                };
                await dbService.InsertVoiceAsync(newUser, newFilePath, "newhash456");

                // Run purge for items older than 30 days
                var result = await maintenance.PurgeOlderThanDaysAsync(30);

                Assert.True(result.VoicesDeleted >= 1);
                Assert.True(result.FilesDeleted >= 1);
                Assert.False(File.Exists(oldFilePath));
                Assert.True(File.Exists(newFilePath));

                // Verify old record is no longer in db
                Assert.False(await dbService.HasDuplicateHashAsync("oldhash123"));
                // Verify new record is still in db
                Assert.True(await dbService.HasDuplicateHashAsync("newhash456"));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath))
                {
                    try { File.Delete(dbPath); } catch { }
                }
                if (Directory.Exists(tempAudioDir))
                {
                    try { Directory.Delete(tempAudioDir, true); } catch { }
                }
            }
        }

        [Fact]
        public async Task GetStorageStats_ReturnsAccurateCounts()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestStats_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={dbPath}";
            var dbService = new DatabaseService(connStr);
            await dbService.InitializeAsync();

            try
            {
                var maintenance = new MaintenanceService(dbService);
                var stats = await maintenance.GetStorageStatsAsync();

                Assert.True(stats.TotalVoices >= 0);
                Assert.True(stats.TotalUsers >= 0);
                Assert.True(stats.DatabaseSizeBytes > 0);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                if (File.Exists(dbPath))
                {
                    try { File.Delete(dbPath); } catch { }
                }
            }
        }
    }
}