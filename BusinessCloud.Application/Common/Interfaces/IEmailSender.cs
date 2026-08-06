namespace BusinessCloud.Application.Common.Interfaces;

public record EmailSendResult(bool Success, string? MessageId, string? ErrorCode, string? ErrorMessage);

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
