using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectVoiceLink
{
    public record RateLimitResult(bool Allowed, int RetryAfterSeconds);

    public class SpamProtectionService
    {
        private readonly DatabaseService _databaseService;
        private readonly ConcurrentDictionary<string, DateTime> _lastVoiceTime = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastCommandTime = new();

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
            if (string.IsNullOrWhiteSpace(userId)) return new RateLimitResult(true, 0);

            var cooldown = Configuration.VoiceCooldownSeconds;
            if (cooldown <= 0) return new RateLimitResult(true, 0);

            var now = DateTime.UtcNow;
            if (_lastVoiceTime.TryGetValue(userId, out var lastTime))
            {
                var elapsed = (now - lastTime).TotalSeconds;
                if (elapsed < cooldown)
                {
                    var retryAfter = (int)Math.Ceiling(cooldown - elapsed);
                    return new RateLimitResult(false, retryAfter);
                }
            }

            _lastVoiceTime[userId] = now;
            return new RateLimitResult(true, 0);
        }

        public RateLimitResult CheckCommandRateLimit(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new RateLimitResult(true, 0);

            var cooldown = Configuration.CommandCooldownSeconds;
            if (cooldown <= 0) return new RateLimitResult(true, 0);

            var now = DateTime.UtcNow;
            if (_lastCommandTime.TryGetValue(userId, out var lastTime))
            {
                var elapsed = (now - lastTime).TotalSeconds;
                if (elapsed < cooldown)
                {
                    var retryAfter = (int)Math.Ceiling(cooldown - elapsed);
                    return new RateLimitResult(false, retryAfter);
                }
            }

            _lastCommandTime[userId] = now;
            return new RateLimitResult(true, 0);
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