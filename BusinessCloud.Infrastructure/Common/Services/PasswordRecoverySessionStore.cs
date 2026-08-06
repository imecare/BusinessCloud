using System.Collections.Concurrent;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Infrastructure.Common.Services;

public sealed class PasswordRecoverySessionStore : IPasswordRecoverySessionStore
{
    private readonly ConcurrentDictionary<string, PasswordRecoverySession> _sessions = new();

    public PasswordRecoverySession Create(
        string tenantId,
        string email,
        string companyName,
        string? ownerPhone,
        PasswordRecoveryChannel channel,
        string maskedContact,
        TimeSpan ttl)
    {
        CleanupExpired();

        var session = new PasswordRecoverySession(
            Guid.NewGuid().ToString("N"),
            tenantId,
            email,
            companyName,
            ownerPhone,
            channel,
            maskedContact,
            DateTime.UtcNow.Add(ttl));

        _sessions[session.SessionId] = session;
        return session;
    }

    public bool TryGet(string sessionId, out PasswordRecoverySession session)
    {
        CleanupExpired();
        return _sessions.TryGetValue(sessionId, out session!);
    }

    public bool TryConfirmContact(string sessionId, string contactValue, out PasswordRecoverySession session)
    {
        CleanupExpired();
        if (!_sessions.TryGetValue(sessionId, out session!))
            return false;

        if (session.Channel == PasswordRecoveryChannel.Email)
        {
            var expected = ContactValue.NormalizeEmail(session.Email);
            if (!string.Equals(expected, ContactValue.NormalizeEmail(contactValue), StringComparison.Ordinal))
                return false;
        }
        else
        {
            var expected = ContactValue.NormalizePhoneDigits(session.OwnerPhone);
            if (!string.Equals(expected, ContactValue.NormalizePhoneDigits(contactValue), StringComparison.Ordinal))
                return false;
        }

        session.ContactConfirmed = true;
        return true;
    }

    public bool TryAttachVerification(string sessionId, string challengeId, string code, out PasswordRecoverySession session)
    {
        CleanupExpired();
        if (!_sessions.TryGetValue(sessionId, out session!))
            return false;

        session.VerificationChallengeId = challengeId;
        session.VerificationCode = code;
        session.VerificationCreatedAtUtc = DateTime.UtcNow;
        session.CodeDelivered = false;
        return true;
    }

    public bool TryGetCodeForWhatsApp(string sessionId, string senderPhone, out PasswordRecoverySession session)
    {
        CleanupExpired();
        if (!_sessions.TryGetValue(sessionId, out session!))
            return false;

        if (session.Channel != PasswordRecoveryChannel.WhatsApp || !session.ContactConfirmed)
            return false;

        var expectedPhone = ContactValue.NormalizePhoneDigits(session.OwnerPhone);
        var senderPhoneDigits = ContactValue.NormalizePhoneDigits(senderPhone);
        if (!string.Equals(expectedPhone, senderPhoneDigits, StringComparison.Ordinal))
            return false;

        return !string.IsNullOrWhiteSpace(session.VerificationCode);
    }

    public bool TryMarkCodeDelivered(string sessionId)
    {
        CleanupExpired();
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        session.CodeDelivered = true;
        return true;
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var pair in _sessions)
        {
            if (pair.Value.ExpiresAtUtc <= now)
            {
                _sessions.TryRemove(pair.Key, out _);
            }
        }
    }

    private static class ContactValue
    {
        public static string NormalizeEmail(string? email)
            => (email ?? string.Empty).Trim().ToLowerInvariant();

        public static string NormalizePhoneDigits(string? phone)
            => new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
    }
}
