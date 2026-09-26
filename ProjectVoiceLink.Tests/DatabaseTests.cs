using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ProjectVoiceLink;
using Xunit;

namespace ProjectVoiceLink.Tests
{
    public class DatabaseTests
    {
        [Fact]
        public async Task Database_CanInsertAndRetrieveVoices()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestVoiceLink_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={dbPath}";
            var dbService = new DatabaseService(connStr);
            await dbService.InitializeAsync();

            try
            {
                var user = new UserClass
                {
                    first_name = "O'Connor",
                    last_name = "Test \"Quotes\"",
                    username = "testuser",
                    id = "123456",
                    languagecode = "en",
                    date = DateTime.UtcNow.ToString("o"),
                    isbot = "False",
                    messageid = "42"
                };

                await dbService.InsertVoiceAsync(user, "audio_bottles/sample.ogg", "abc123md5hash");

                var exists = await dbService.HasDuplicateHashAsync("abc123md5hash");
                Assert.True(exists);

                var nonExistent = await dbService.HasDuplicateHashAsync("nonexistenthash");
                Assert.False(nonExistent);
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

        [Fact]
        public async Task Database_HandlesHighConcurrencyWithoutLockErrors()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestConcurrency_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={dbPath}";
            var dbService = new DatabaseService(connStr);
            await dbService.InitializeAsync();

            try
            {
                // Run 25 parallel insert and read operations simultaneously
                var tasks = Enumerable.Range(0, 25).Select(async i =>
                {
                    var user = new UserClass
                    {
                        first_name = $"User_{i}",
                        id = i.ToString(),
                        username = $"concurrent_{i}",
                        date = DateTime.UtcNow.ToString("o")
                    };

                    await dbService.InsertVoiceAsync(user, $"audio_bottles/concurrent_{i}.ogg", $"hash_{i}");
                    await dbService.LogMessageAsync(user);
                    var hasDup = await dbService.HasDuplicateHashAsync($"hash_{i}");
                    Assert.True(hasDup);
                });

                await Task.WhenAll(tasks);

                var stats = await dbService.GetStatsAsync();
                Assert.Equal(25, stats.TotalVoices);
                Assert.Equal(25, stats.TotalLogs);
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

        [Fact]
        public async Task Database_UserPreferredLanguage_CanBeSetAndRetrieved()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestLang_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={dbPath}";
            var dbService = new DatabaseService(connStr);
            await dbService.InitializeAsync();

            try
            {
                var userId = "user_lang_test_123";
                var initial = await dbService.GetUserPreferredLanguageAsync(userId);
                Assert.Null(initial);

                await dbService.SetUserPreferredLanguageAsync(userId, "en");
                var updated = await dbService.GetUserPreferredLanguageAsync(userId);
                Assert.Equal("en", updated);

                await dbService.SetUserPreferredLanguageAsync(userId, "es");
                var updated2 = await dbService.GetUserPreferredLanguageAsync(userId);
                Assert.Equal("es", updated2);
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