using System.Collections.Concurrent;
using System.Security.Cryptography;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Infrastructure.Common.Services;

/// <summary>
/// Servicio de códigos de verificación (OTP) en memoria, con expiración y límite de intentos.
/// Adecuado para una sola instancia. Para múltiples instancias, respaldar en Redis.
/// </summary>
public class VerificationCodeService : IVerificationCodeService
{
    private sealed class Entry
    {
        public required string Code { get; init; }
        public required string Purpose { get; init; }
        public required string SubjectId { get; init; }
        public required DateTime ExpiresAtUtc { get; init; }
        public int Attempts;
    }

    private const int MaxAttempts = 5;
    private readonly ConcurrentDictionary<string, Entry> _store = new();

    public (string ChallengeId, string Code) Create(string purpose, string subjectId, TimeSpan ttl)
    {
        CleanupExpired();

        var challengeId = Guid.NewGuid().ToString("N");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _store[challengeId] = new Entry
        {
            Code = code,
            Purpose = purpose,
            SubjectId = subjectId,
            ExpiresAtUtc = DateTime.UtcNow.Add(ttl),
        };

        return (challengeId, code);
    }

    public bool Validate(string challengeId, string code, string purpose, string subjectId)
    {
        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code))
            return false;

        if (!_store.TryGetValue(challengeId, out var entry))
            return false;

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            _store.TryRemove(challengeId, out _);
            return false;
        }

        if (Interlocked.Increment(ref entry.Attempts) > MaxAttempts)
        {
            _store.TryRemove(challengeId, out _);
            return false;
        }

        var ok = entry.Purpose == purpose
            && entry.SubjectId == subjectId
            && FixedTimeEquals(entry.Code, code);

        if (ok)
        {
            // Un solo uso: consumir el desafío.
            _store.TryRemove(challengeId, out _);
        }

        return ok;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _store)
        {
            if (kvp.Value.ExpiresAtUtc < now)
                _store.TryRemove(kvp.Key, out _);
        }
    }
}
