using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.ChangeSuperAdminPhone;

/// <summary>
/// Confirma el cambio del teléfono del super administrador con el código recibido por WhatsApp.
/// </summary>
public record ConfirmSuperAdminPhoneChangeCommand(string ChallengeId, string Code, string NewPhone)
    : IRequest<string>;

public class ConfirmSuperAdminPhoneChangeHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser,
    IVerificationCodeService verification)
    : IRequestHandler<ConfirmSuperAdminPhoneChangeCommand, string>
{
    private const string DefaultSuperAdminPhone = "3121232192";

    public async Task<string> Handle(
        ConfirmSuperAdminPhoneChangeCommand request,
        CancellationToken cancellationToken)
    {
        var newPhone = new string((request.NewPhone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (newPhone.Length is < 10 or > 15)
            throw new ArgumentException("El nuevo número debe tener entre 10 y 15 dígitos.");

        var subject = currentUser.UserId ?? "platform";
        var valid = verification.Validate(
            request.ChallengeId,
            request.Code,
            RequestSuperAdminPhoneChangeHandler.Purpose,
            subject);

        if (!valid)
            throw new ArgumentException("El código de confirmación es inválido o expiró.");

        var settings = await context.PlatformSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new PlatformSettings { Id = 1, SuperAdminPhone = DefaultSuperAdminPhone };
            context.PlatformSettings.Add(settings);
        }

        settings.SuperAdminPhone = newPhone;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);
        return newPhone;
    }
}
