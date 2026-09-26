using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectVoiceLink
{
    public static class Configuration
    {
        public static int minimum_voice_message_length_in_seconds => GetInt("MIN_VOICE_LENGTH_SECONDS", 1);

        public static int MaximumVoiceDurationSeconds => GetInt("MAX_VOICE_LENGTH_SECONDS", 180);

        public static int VoiceCooldownSeconds => GetInt("VOICE_COOLDOWN_SECONDS", 3);

        public static int VoiceWindowSeconds => GetInt("VOICE_WINDOW_SECONDS", 30);

        public static int VoiceMaxPerWindow => GetInt("VOICE_MAX_PER_WINDOW", 3);

        public static int CommandCooldownSeconds => GetInt("COMMAND_COOLDOWN_SECONDS", 1);

        public static int CommandWindowSeconds => GetInt("COMMAND_WINDOW_SECONDS", 10);

        public static int CommandMaxPerWindow => GetInt("COMMAND_MAX_PER_WINDOW", 5);

        public static int RetentionDays => GetInt("RETENTION_DAYS", 30);

        public static int MaintenanceIntervalHours => GetInt("MAINTENANCE_INTERVAL_HOURS", 24);

        public static string AudioStoragePath => GetAudioStoragePath();

        public static string DatabasePath => GetDatabaseConnectionString();

        public static string BotToken => GetBotToken();

        public static HashSet<string> AdminUserIds => GetIdSet("ADMIN_USER_IDS");

        public static HashSet<string> BannedUserIds => GetIdSet("BANNED_USER_IDS");

        private static int GetInt(string envName, int defaultValue)
        {
            var envVal = Environment.GetEnvironmentVariable(envName);
            if (!string.IsNullOrWhiteSpace(envVal) && int.TryParse(envVal, out int val) && val >= 0)
            {
                return val;
            }
            return defaultValue;
        }

        private static HashSet<string> GetIdSet(string envName)
        {
            var envVal = Environment.GetEnvironmentVariable(envName);
            if (string.IsNullOrWhiteSpace(envVal))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return envVal
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string GetAudioStoragePath()
        {
            var envVal = Environment.GetEnvironmentVariable("AUDIO_STORAGE_PATH");
            if (!string.IsNullOrWhiteSpace(envVal))
            {
                return envVal;
            }
            return "audio_bottles";
        }

        private static string GetDatabaseConnectionString()
        {
            var envVal = Environment.GetEnvironmentVariable("DATABASE_PATH");
            if (!string.IsNullOrWhiteSpace(envVal))
            {
                if (envVal.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    return envVal;
                }
                return $"Data Source={envVal}";
            }
            return "Data Source=ProjectVoiceLink.db";
        }

        private static string GetBotToken()
        {
            var envVal = Environment.GetEnvironmentVariable("BOT_TOKEN") 
                      ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            if (!string.IsNullOrWhiteSpace(envVal))
            {
                return envVal;
            }
            return "YOURBOTTOKEN";
        }
    }
}