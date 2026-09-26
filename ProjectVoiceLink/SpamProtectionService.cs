using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectVoiceLink
{
    public record RateLimitResult(bool Allowed, int RetryAfterSeconds);

    public class SpamProtectionService
    {
        private readonly DatabaseService _databaseService;
        private readonly ConcurrentDictionary<string, List<DateTime>> _voiceHistory = new();
        private readonly ConcurrentDictionary<string, List<DateTime>> _commandHistory = new();

        public SpamProtectionService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public bool IsUserBanned(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            return Configuration.BannedUserIds.Contains(userId);
        }

        public RateLimitResult CheckVoiceRateLimit(string userId)
        {
            return CheckRateLimit(
                userId, 
                _voiceHistory, 
                Configuration.VoiceCooldownSeconds, 
                Configuration.VoiceWindowSeconds, 
                Configuration.VoiceMaxPerWindow);
        }

        public RateLimitResult CheckCommandRateLimit(string userId)
        {
            return CheckRateLimit(
                userId, 
                _commandHistory, 
                Configuration.CommandCooldownSeconds, 
                Configuration.CommandWindowSeconds, 
                Configuration.CommandMaxPerWindow);
        }

        private static RateLimitResult CheckRateLimit(
            string userId,
            ConcurrentDictionary<string, List<DateTime>> history,
            int cooldownSec,
            int windowSec,
            int maxPerWindow)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new RateLimitResult(true, 0);

            // If rate limiting is completely disabled
            if (cooldownSec <= 0 && (windowSec <= 0 || maxPerWindow <= 0))
            {
                return new RateLimitResult(true, 0);
            }

            var now = DateTime.UtcNow;
            var list = history.GetOrAdd(userId, _ => new List<DateTime>());

            lock (list)
            {
                // 1. Minimum spacing cooldown between consecutive messages
                if (cooldownSec > 0 && list.Count > 0)
                {
                    var lastTime = list[^1];
                    var elapsed = (now - lastTime).TotalSeconds;
                    if (elapsed < cooldownSec)
                    {
                        var retryAfter = (int)Math.Ceiling(cooldownSec - elapsed);
                        return new RateLimitResult(false, Math.Max(1, retryAfter));
                    }
                }

                // 2. Sliding window limit (maximum N submissions within window W seconds)
                if (windowSec > 0 && maxPerWindow > 0)
                {
                    var windowCutoff = now.AddSeconds(-windowSec);
                    list.RemoveAll(t => t < windowCutoff);

                    if (list.Count >= maxPerWindow)
                    {
                        var oldestInWindow = list[0];
                        var timeUntilExpiry = (oldestInWindow.AddSeconds(windowSec) - now).TotalSeconds;
                        var retryAfter = (int)Math.Ceiling(timeUntilExpiry);
                        return new RateLimitResult(false, Math.Max(1, retryAfter));
                    }
                }

                list.Add(now);
                return new RateLimitResult(true, 0);
            }
        }

        public bool ValidateVoiceDuration(int durationSeconds, out string? errorReason, out int boundaryValue)
        {
            var min = Configuration.minimum_voice_message_length_in_seconds;
            var max = Configuration.MaximumVoiceDurationSeconds;

            if (durationSeconds < min)
            {
                errorReason = "too_short";
                boundaryValue = min;
                return false;
            }

            if (max > 0 && durationSeconds > max)
            {
                errorReason = "too_long";
                boundaryValue = max;
                return false;
            }

            errorReason = null;
            boundaryValue = 0;
            return true;
        }

        public async Task<bool> IsDuplicateVoiceAsync(string filehash, CancellationToken cancellationToken = default)
        {
            return await _databaseService.HasDuplicateHashAsync(filehash, cancellationToken);
        }
    }
}