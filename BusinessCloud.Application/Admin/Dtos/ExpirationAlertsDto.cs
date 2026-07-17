using BusinessCloud.Application.Admin.Dtos;

namespace BusinessCloud.Application.Admin.Dtos;

/// <summary>
/// Resumen de vencimientos para el panel de avisos del admin.
/// </summary>
public class ExpirationAlertsDto
{
    public int ActiveCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int GraceCount { get; set; }
    public int SuspendedCount { get; set; }
    public int TotalWithSubscription { get; set; }

    /// <summary>Empresas que requieren atención (por vencer, en prórroga o suspendidas).</summary>
    public IReadOnlyList<CompanyListItemDto> Companies { get; set; } = new List<CompanyListItemDto>();
}
