using System;
using System.Collections.Concurrent;

namespace SnakesAndLadders.Services.Logic.Auth
{
    public sealed class VerificationCodeStore : IVerificationCodeStore
    {
        private const int VerificationMaxFailedAttempts = 5;
        private const int FailedAttemptsInitialValue = 0;
        private const int VerificationTtlMinutes = 10;

        private static readonly TimeSpan VerificationTtl = TimeSpan.FromMinutes(VerificationTtlMinutes);

        private static readonly ConcurrentDictionary<string, VerificationCodeEntry> _cache =
            new ConcurrentDictionary<string, VerificationCodeEntry>(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string email, out VerificationCodeEntry entry)
        {
            return _cache.TryGetValue(email, out entry);
        }

        public void SaveNewCode(string email, string code, DateTime nowUtc)
        {
            var entry = new VerificationCodeEntry
            {
                Code = code,
                ExpiresUtc = nowUtc.Add(VerificationTtl),
                LastSentUtc = nowUtc,
                FailedAttempts = FailedAttemptsInitialValue
            };

            _cache[email] = entry;
        }

        public void Remove(string email)
        {
            _cache.TryRemove(email, out _);
        }

        public VerificationCodeEntry RegisterFailedAttempt(string email, VerificationCodeEntry currentEntry)
        {
            int newFailedAttempts = currentEntry.FailedAttempts + 1;

            var updated = new VerificationCodeEntry
            {
                Code = currentEntry.Code,
                ExpiresUtc = currentEntry.ExpiresUtc,
                LastSentUtc = currentEntry.LastSentUtc,
                FailedAttempts = newFailedAttempts
            };

            _cache[email] = updated;

            if (newFailedAttempts >= VerificationMaxFailedAttempts)
            {
                _cache.TryRemove(email, out _);
            }

            return updated;
        }
    }
}
