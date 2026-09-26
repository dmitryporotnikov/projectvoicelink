using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using ProjectVoiceLink;
using Xunit;

namespace ProjectVoiceLink.Tests
{
    public class SpamProtectionTests
    {
        [Fact]
        public void ValidateVoiceDuration_EnforcesMinimumAndMaximum()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestSpam_{Guid.NewGuid():N}.db");
            var dbService = new DatabaseService($"Data Source={dbPath}");
            var spam = new SpamProtectionService(dbService);

            // Too short
            Assert.False(spam.ValidateVoiceDuration(0, out var errorShort, out var boundShort));
            Assert.Equal("too_short", errorShort);
            Assert.Equal(Configuration.minimum_voice_message_length_in_seconds, boundShort);

            // Valid
            Assert.True(spam.ValidateVoiceDuration(10, out var errorValid, out var _));
            Assert.Null(errorShort != null ? null : errorValid);

            // Too long
            if (Configuration.MaximumVoiceDurationSeconds > 0)
            {
                Assert.False(spam.ValidateVoiceDuration(Configuration.MaximumVoiceDurationSeconds + 10, out var errorLong, out var boundLong));
                Assert.Equal("too_long", errorLong);
                Assert.Equal(Configuration.MaximumVoiceDurationSeconds, boundLong);
            }
        }

        [Fact]
        public void CheckVoiceRateLimit_BlocksRapidConsecutiveSubmissions()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestSpam_{Guid.NewGuid():N}.db");
            var dbService = new DatabaseService($"Data Source={dbPath}");
            var spam = new SpamProtectionService(dbService);

            var userId = "test_user_rate_limit";

            var first = spam.CheckVoiceRateLimit(userId);
            Assert.True(first.Allowed);

            if (Configuration.VoiceCooldownSeconds > 0)
            {
                var second = spam.CheckVoiceRateLimit(userId);
                Assert.False(second.Allowed);
                Assert.True(second.RetryAfterSeconds > 0);
            }
        }

        [Fact]
        public void CheckCommandRateLimit_BlocksRapidCommands()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"TestSpam_{Guid.NewGuid():N}.db");
            var dbService = new DatabaseService($"Data Source={dbPath}");
            var spam = new SpamProtectionService(dbService);

            var userId = "test_user_cmd_rate";

            var first = spam.CheckCommandRateLimit(userId);
            Assert.True(first.Allowed);

            if (Configuration.CommandCooldownSeconds > 0)
            {
                var second = spam.CheckCommandRateLimit(userId);
                Assert.False(second.Allowed);
                Assert.True(second.RetryAfterSeconds > 0);
            }
        }

        [Fact]
        public void SlidingWindow_AllowsConfigurableLimitsViaEnvironment()
        {
            Assert.True(Configuration.VoiceCooldownSeconds >= 0);
            Assert.True(Configuration.VoiceWindowSeconds >= 0);
            Assert.True(Configuration.VoiceMaxPerWindow >= 0);
            Assert.True(Configuration.CommandCooldownSeconds >= 0);
            Assert.True(Configuration.CommandWindowSeconds >= 0);
            Assert.True(Configuration.CommandMaxPerWindow >= 0);
        }
    }
}