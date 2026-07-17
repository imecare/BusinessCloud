using MediatR;

namespace BusinessCloud.Application.Admin.Commands.SetCompanyStatus;

/// <summary>
/// Activa o suspende una empresa. Al desactivar, se marca la suscripción como suspendida
/// manualmente (los usuarios no podrán operar); al activar, se retira la suspensión manual.
/// </summary>
public record SetCompanyStatusCommand(string TenantId, bool IsActive)
    : IRequest<SetCompanyStatusResult>;

public record SetCompanyStatusResult(string TenantId, bool IsActive, string Status);
