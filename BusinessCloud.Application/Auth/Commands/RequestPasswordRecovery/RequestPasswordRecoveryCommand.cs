using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Common.Utilities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Auth.Commands.RequestPasswordRecovery;

public sealed record RequestPasswordRecoveryCommand(string Email, PasswordRecoveryChannel Channel)
    : IRequest<RequestPasswordRecoveryResult>;

public sealed record RequestPasswordRecoveryResult(
    bool Success,
    string SessionId,
    string ChallengeId,
    string TenantId,
    string CompanyName,
    string MaskedContact,
    PasswordRecoveryChannel Channel,
    int ExpiresInSeconds,
    bool RequiresConfirmation,
    bool Delivered,
    string Message,
    string? SentTo);

public sealed class RequestPasswordRecoveryHandler(
    IIdentityDbContext identityContext,
    IPasswordRecoverySessionStore sessionStore,
    IVerificationCodeService verificationCodeService,
    IEmailSender emailSender)
    : IRequestHandler<RequestPasswordRecoveryCommand, RequestPasswordRecoveryResult>
{
    private const string Purpose = "password.recovery";

    public async Task<RequestPasswordRecoveryResult> Handle(RequestPasswordRecoveryCommand request, CancellationToken cancellationToken)
    {
        var email = ContactMasker.NormalizeEmail(request.Email);
        var user = await identityContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Email != null
                    && u.Email.ToLower() == email
                    && u.IsActive
                    && (u.Role == "SuperAdmin" || u.Role == "PlatformAdmin"),
                cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("No se encontro una empresa activa con ese correo.");

        var companyName = string.IsNullOrWhiteSpace(user.TenantId)
            ? "BusinessCloud"
            : await identityContext.Tenants
                .AsNoTracking()
                .Where(t => t.Id == user.TenantId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? user.TenantId;

        var subscription = await identityContext.TenantSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == user.TenantId, cancellationToken);

        var ownerPhone = ContactMasker.NormalizePhoneDigits(subscription?.OwnerPhone);
        if (request.Channel == PasswordRecoveryChannel.WhatsApp && string.IsNullOrWhiteSpace(ownerPhone))
            throw new InvalidOperationException("La empresa no tiene un telefono de propietario registrado para WhatsApp.");

        var maskedContact = request.Channel == PasswordRecoveryChannel.Email
            ? ContactMasker.MaskEmail(email)
            : ContactMasker.MaskPhone(ownerPhone);

        var session = sessionStore.Create(
            user.TenantId ?? string.Empty,
            email,
            companyName,
            ownerPhone,
            request.Channel,
            maskedContact,
            TimeSpan.FromMinutes(5));

        if (request.Channel == PasswordRecoveryChannel.Email && user.Role == "PlatformAdmin")
        {
            var (challengeId, code) = verificationCodeService.Create(Purpose, session.Email, TimeSpan.FromMinutes(5));
            if (!sessionStore.TryAttachVerification(session.SessionId, challengeId, code, out _))
                throw new KeyNotFoundException("La sesion de recuperacion no existe o expiro.");

            var subject = $"Recuperacion de contrasena - {companyName}";
            var html = $"<p>Hola,</p><p>Tu codigo de verificacion es: <strong>{code}</strong></p><p>Vence en 5 minutos.</p>";
            var sendResult = await emailSender.SendAsync(session.Email, subject, html, cancellationToken);

            return new RequestPasswordRecoveryResult(
                sendResult.Success,
                session.SessionId,
                challengeId,
                user.TenantId ?? string.Empty,
                companyName,
                maskedContact,
                request.Channel,
                300,
                false,
                sendResult.Success,
                sendResult.Success
                    ? "Enviamos el codigo al correo registrado."
                    : string.IsNullOrWhiteSpace(sendResult.ErrorMessage)
                        ? "No se pudo enviar el correo de recuperacion."
                        : $"No se pudo enviar el correo de recuperacion. {sendResult.ErrorMessage}",
                session.Email);
        }

        return new RequestPasswordRecoveryResult(
            true,
            session.SessionId,
            session.SessionId,
            user.TenantId ?? string.Empty,
            companyName,
            maskedContact,
            request.Channel,
            300,
            true,
            false,
            request.Channel == PasswordRecoveryChannel.Email
                ? "Confirma tu correo para enviar el codigo de recuperacion."
                : "Confirma tu numero de WhatsApp para mostrar el QR de recuperacion.",
            null);
    }
}

public sealed class RequestPasswordRecoveryValidator : AbstractValidator<RequestPasswordRecoveryCommand>
{
    public RequestPasswordRecoveryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Channel)
            .IsInEnum();
    }
}
