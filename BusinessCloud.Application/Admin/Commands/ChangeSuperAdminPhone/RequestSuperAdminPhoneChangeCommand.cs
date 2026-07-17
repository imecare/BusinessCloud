using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.ChangeSuperAdminPhone;

/// <summary>
/// Solicita el cambio del teléfono del super administrador: genera un código de confirmación
/// y lo envía por WhatsApp al teléfono ACTUAL. Devuelve el identificador del desafío.
/// </summary>
public record RequestSuperAdminPhoneChangeCommand : IRequest<RequestPhoneChangeResult>;

public record RequestPhoneChangeResult(string ChallengeId, bool CodeSent);

public class RequestSuperAdminPhoneChangeHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser,
    IVerificationCodeService verification,
    IWhatsAppSender whatsApp)
    : IRequestHandler<RequestSuperAdminPhoneChangeCommand, RequestPhoneChangeResult>
{
    public const string Purpose = "platform.phone.change";
    private const string DefaultSuperAdminPhone = "3121232192";

    public async Task<RequestPhoneChangeResult> Handle(
        RequestSuperAdminPhoneChangeCommand request,
        CancellationToken cancellationToken)
    {
        var currentPhone = await context.PlatformSettings
            .AsNoTracking()
            .Select(s => s.SuperAdminPhone)
            .FirstOrDefaultAsync(cancellationToken) ?? DefaultSuperAdminPhone;

        var subject = currentUser.UserId ?? "platform";
        var (challengeId, code) = verification.Create(Purpose, subject, TimeSpan.FromMinutes(10));

        var message =
            $"🔐 Código para cambiar el teléfono del super administrador: {code}\n" +
            "Vence en 10 minutos. Si no solicitaste el cambio, ignora este mensaje.";

        var sent = false;
        try
        {
            sent = await whatsApp.SendTextAsync(currentPhone, message, cancellationToken);
        }
        catch
        {
            sent = false;
        }

        return new RequestPhoneChangeResult(challengeId, sent);
    }
}
