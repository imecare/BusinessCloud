using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Common.Utilities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace BusinessCloud.Application.Auth.Commands.ConfirmPasswordRecoveryContact;

public sealed record ConfirmPasswordRecoveryContactCommand(string SessionId, string ContactValue)
    : IRequest<ConfirmPasswordRecoveryContactResult>;

public sealed record ConfirmPasswordRecoveryContactResult(
    string SessionId,
    string ChallengeId,
    PasswordRecoveryChannel Channel,
    int ExpiresInSeconds,
    string MaskedContact,
    bool Delivered,
    string Message,
    string? WhatsAppChatUrl,
    string? WhatsAppQrUrl);

public sealed class ConfirmPasswordRecoveryContactHandler(
    IPasswordRecoverySessionStore sessionStore,
    IVerificationCodeService verificationCodeService,
    IEmailSender emailSender,
    IConfiguration configuration)
    : IRequestHandler<ConfirmPasswordRecoveryContactCommand, ConfirmPasswordRecoveryContactResult>
{
    private const string Purpose = "password.recovery";

    public async Task<ConfirmPasswordRecoveryContactResult> Handle(ConfirmPasswordRecoveryContactCommand request, CancellationToken cancellationToken)
    {
        if (!sessionStore.TryConfirmContact(request.SessionId, request.ContactValue, out var session))
            throw new KeyNotFoundException("La sesion de recuperacion no existe, expiro o el dato no coincide.");

        var (challengeId, code) = verificationCodeService.Create(Purpose, session.Email, TimeSpan.FromMinutes(5));
        if (!sessionStore.TryAttachVerification(session.SessionId, challengeId, code, out _))
            throw new KeyNotFoundException("La sesion de recuperacion no existe o expiro.");

        if (session.Channel == PasswordRecoveryChannel.Email)
        {
            var subject = $"Recuperacion de contrasena - {session.CompanyName}";
            var html = $"<p>Hola,</p><p>Tu codigo de verificacion es: <strong>{code}</strong></p><p>Vence en 5 minutos.</p>";
            var sendResult = await emailSender.SendAsync(session.Email, subject, html, cancellationToken);

            return new ConfirmPasswordRecoveryContactResult(
                session.SessionId,
                challengeId,
                session.Channel,
                300,
                session.MaskedContact,
                sendResult.Success,
                sendResult.Success
                    ? "Enviamos el codigo al correo registrado."
                    : "No se pudo enviar el correo de recuperacion.",
                null,
                null);
        }

        var publicNumber = configuration["WhatsApp:PublicNumber"];
        if (string.IsNullOrWhiteSpace(publicNumber))
        {
            return new ConfirmPasswordRecoveryContactResult(
                session.SessionId,
                challengeId,
                session.Channel,
                300,
                session.MaskedContact,
                false,
                "Configura WhatsApp:PublicNumber para mostrar el QR de recuperacion.",
                null,
                null);
        }

        var message = $"RECUPERAR CONTRASENA {session.SessionId}";
        var chatUrl = $"https://wa.me/{ContactMasker.NormalizePhoneDigits(publicNumber)}?text={Uri.EscapeDataString(message)}";
        var qrUrl = $"https://quickchart.io/qr?size=240&text={Uri.EscapeDataString(chatUrl)}";

        return new ConfirmPasswordRecoveryContactResult(
            session.SessionId,
            challengeId,
            session.Channel,
            300,
            session.MaskedContact,
            true,
            "Abre el QR desde el telefono registrado y envia el mensaje al WhatsApp oficial. El codigo llegara por ese mismo chat.",
            chatUrl,
            qrUrl);
    }
}

public sealed class ConfirmPasswordRecoveryContactValidator : AbstractValidator<ConfirmPasswordRecoveryContactCommand>
{
    public ConfirmPasswordRecoveryContactValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ContactValue).NotEmpty();
    }
}
