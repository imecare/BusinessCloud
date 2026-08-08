using Azure;
using Azure.Communication.Email;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Infrastructure.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmailSendResult = BusinessCloud.Application.Common.Interfaces.EmailSendResult;

namespace BusinessCloud.Infrastructure.Common.Services;

public sealed class AzureEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<AzureEmailSender> _logger;
    private readonly Lazy<EmailClient?> _client;

    public AzureEmailSender(IOptions<EmailOptions> options, ILogger<AzureEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<EmailClient?>(CreateClient);
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new EmailSendResult(false, null, "MissingConfiguration", BuildMissingConfigurationMessage());

        var client = _client.Value;
        if (client is null)
            return new EmailSendResult(false, null, "MissingConfiguration", BuildMissingConfigurationMessage());

        try
        {
            var content = new EmailContent(subject) { Html = htmlBody };
            var recipients = new EmailRecipients(new List<EmailAddress> { new(to) });
            var message = new EmailMessage(_options.SenderAddress, recipients, content);

            var operation = await client.SendAsync(WaitUntil.Completed, message, cancellationToken);
            return new EmailSendResult(true, operation.Id, null, null);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Fallo el envio de correo con ACS.");
            return new EmailSendResult(false, null, ex.ErrorCode ?? "EmailSendFailed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar correo.");
            return new EmailSendResult(false, null, "InternalError", ex.Message);
        }
    }

    private EmailClient? CreateClient()
    {
        if (!IsConfigured)
            return null;

        return new EmailClient(_options.ConnectionString);
    }

    private static string BuildMissingConfigurationMessage() =>
        "La configuracion de correo no esta completa. Revisa Email:ConnectionString y Email:SenderAddress en appsettings.";
}
