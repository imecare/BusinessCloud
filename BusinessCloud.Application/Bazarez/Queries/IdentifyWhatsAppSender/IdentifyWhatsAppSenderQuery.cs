using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.IdentifyWhatsAppSender;

public static class WhatsAppSenderRole
{
    public const int Unknown = 0;
    public const int Owner = 1;
    public const int Customer = 2;
}

public record OwnerWhatsAppTenantDto(string TenantId, string CompanyName, string BazarName);

public record CustomerWhatsAppAccountDto(
    int ClosureCustomerTotalId,
    string TenantId,
    string BazarName,
    decimal TotalAmount,
    string UploadToken,
    int Status);

public class IdentifyWhatsAppSenderResultDto
{
    public string NormalizedPhone { get; set; } = string.Empty;
    public int Role { get; set; }
    public List<OwnerWhatsAppTenantDto> OwnerTenants { get; set; } = new();
    public List<CustomerWhatsAppAccountDto> CustomerAccounts { get; set; } = new();
}

public record IdentifyWhatsAppSenderQuery(string Phone) : IRequest<IdentifyWhatsAppSenderResultDto>;

public class IdentifyWhatsAppSenderHandler(IIdentityDbContext identityContext, IBazaresDbContext bazaresContext)
    : IRequestHandler<IdentifyWhatsAppSenderQuery, IdentifyWhatsAppSenderResultDto>
{
    public async Task<IdentifyWhatsAppSenderResultDto> Handle(IdentifyWhatsAppSenderQuery request, CancellationToken cancellationToken)
    {
        var candidates = BuildPhoneCandidates(request.Phone);
        var normalizedPhone = candidates.FirstOrDefault() ?? string.Empty;

        if (candidates.Count == 0)
        {
            return new IdentifyWhatsAppSenderResultDto();
        }

        var ownerRows = await identityContext.TenantSubscriptions
            .AsNoTracking()
            .Where(s => s.OwnerPhone != null && candidates.Contains(s.OwnerPhone))
            .Select(s => new { s.TenantId, CompanyName = s.Tenant != null ? s.Tenant.Name : s.TenantId })
            .ToListAsync(cancellationToken);

        var ownerTenantIds = ownerRows.Select(x => x.TenantId).Distinct().ToList();
        var bazarNames = ownerTenantIds.Count == 0
            ? new Dictionary<string, string>()
            : await bazaresContext.BazarSettings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => ownerTenantIds.Contains(s.TenantId))
                .Select(s => new { s.TenantId, s.BazarName })
                .ToDictionaryAsync(x => x.TenantId, x => x.BazarName ?? "Bazar", cancellationToken);

        var owners = ownerRows
            .GroupBy(x => x.TenantId)
            .Select(g =>
            {
                var row = g.First();
                return new OwnerWhatsAppTenantDto(
                    row.TenantId,
                    row.CompanyName,
                    bazarNames.TryGetValue(row.TenantId, out var bazarName) ? bazarName : row.CompanyName);
            })
            .ToList();

        var customerTotals = await bazaresContext.ClosureCustomerTotals
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.Customer)
            .Where(t => candidates.Contains(t.Customer.Phone)
                        && (t.Status == BzaClosureCustomerTotalStatus.Pending
                            || t.Status == BzaClosureCustomerTotalStatus.Rejected))
            .Select(t => new
            {
                t.Id,
                t.TenantId,
                t.TotalAmount,
                t.UploadToken,
                t.Status,
            })
            .ToListAsync(cancellationToken);

        var customerTenantIds = customerTotals.Select(x => x.TenantId).Distinct().ToList();
        var customerBazarNames = customerTenantIds.Count == 0
            ? new Dictionary<string, string>()
            : await bazaresContext.BazarSettings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => customerTenantIds.Contains(s.TenantId))
                .Select(s => new { s.TenantId, s.BazarName })
                .ToDictionaryAsync(x => x.TenantId, x => x.BazarName ?? "Bazar", cancellationToken);

        var accounts = customerTotals
            .Select(t => new CustomerWhatsAppAccountDto(
                t.Id,
                t.TenantId,
                customerBazarNames.TryGetValue(t.TenantId, out var bazarName) ? bazarName : "Bazar",
                t.TotalAmount,
                t.UploadToken,
                t.Status))
            .ToList();

        return new IdentifyWhatsAppSenderResultDto
        {
            NormalizedPhone = normalizedPhone,
            Role = owners.Count > 0 ? WhatsAppSenderRole.Owner : accounts.Count > 0 ? WhatsAppSenderRole.Customer : WhatsAppSenderRole.Unknown,
            OwnerTenants = owners,
            CustomerAccounts = accounts,
        };
    }

    private static List<string> BuildPhoneCandidates(string? phone)
    {
        var digits = NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(digits))
            return new List<string>();

        var candidates = new HashSet<string>(StringComparer.Ordinal)
        {
            digits,
        };

        if (digits.Length == 10)
        {
            candidates.Add("52" + digits);
        }
        else if (digits.StartsWith("52", StringComparison.Ordinal) && digits.Length > 10)
        {
            candidates.Add(digits[2..]);
        }

        return candidates.OrderByDescending(x => x.Length).ToList();
    }

    private static string NormalizePhone(string? phone) => new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
}