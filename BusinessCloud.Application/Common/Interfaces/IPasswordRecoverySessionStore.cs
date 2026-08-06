namespace BusinessCloud.Application.Common.Interfaces;

public enum PasswordRecoveryChannel
{
    Email = 1,
    WhatsApp = 2,
}

public sealed record PasswordRecoverySession(
    string SessionId,
    string TenantId,
    string Email,
    string CompanyName,
    string? OwnerPhone,
    PasswordRecoveryChannel Channel,
    string MaskedContact,
    DateTime ExpiresAtUtc)
{
    public bool ContactConfirmed { get; set; }

    public string? VerificationChallengeId { get; set; }

    public string? VerificationCode { get; set; }

    public DateTime? VerificationCreatedAtUtc { get; set; }

    public bool CodeDelivered { get; set; }
}

public interface IPasswordRecoverySessionStore
{
    PasswordRecoverySession Create(
        string tenantId,
        string email,
        string companyName,
        string? ownerPhone,
        PasswordRecoveryChannel channel,
        string maskedContact,
        TimeSpan ttl);

    bool TryGet(string sessionId, out PasswordRecoverySession session);

    bool TryConfirmContact(string sessionId, string contactValue, out PasswordRecoverySession session);

    bool TryAttachVerification(string sessionId, string challengeId, string code, out PasswordRecoverySession session);

    bool TryGetCodeForWhatsApp(string sessionId, string senderPhone, out PasswordRecoverySession session);

    bool TryMarkCodeDelivered(string sessionId);
}
