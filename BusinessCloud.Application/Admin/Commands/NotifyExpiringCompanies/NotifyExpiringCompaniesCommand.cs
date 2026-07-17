using MediatR;

namespace BusinessCloud.Application.Admin.Commands.NotifyExpiringCompanies;

/// <summary>
/// Envía avisos por WhatsApp a los dueños de las empresas cuya suscripción está por vencer,
/// en prórroga o suspendida. No reenvía a una empresa que ya fue avisada el mismo día.
/// </summary>
public record NotifyExpiringCompaniesCommand(int ExpiringSoonDays = 10)
    : IRequest<NotifyExpiringCompaniesResult>;

public record NotifyExpiringCompaniesResult(
    int Notified,
    int Failed,
    int Skipped,
    IReadOnlyList<NotifyDetail> Details);

public record NotifyDetail(string TenantId, string CompanyName, string Status, bool Sent, string? Error);
