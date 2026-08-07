using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Common.Utilities;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.IdentifyWhatsAppSender;

public static class WhatsAppSenderRole
{
    public const int Unknown = 0;
    public const int Customer = 2;
}

public record CustomerWhatsAppAccountDto(
    int ClosureCustomerTotalId,
    string TenantId,
    string BazarName,
    decimal TotalAmount,
    string UploadToken,
    int Status,
    string? BazarWhatsApp);

public class IdentifyWhatsAppSenderResultDto
{
    public string NormalizedPhone { get; set; } = string.Empty;
    public int Role { get; set; }
    public List<CustomerWhatsAppAccountDto> CustomerAccounts { get; set; } = new();
}

public record IdentifyWhatsAppSenderQuery(string Phone) : IRequest<IdentifyWhatsAppSenderResultDto>;

public class IdentifyWhatsAppSenderHandler(IBazaresDbContext bazaresContext)
    : IRequestHandler<IdentifyWhatsAppSenderQuery, IdentifyWhatsAppSenderResultDto>
{
    public async Task<IdentifyWhatsAppSenderResultDto> Handle(
        IdentifyWhatsAppSenderQuery request,
        CancellationToken cancellationToken)
    {
        var candidates = PhoneNumberCandidates.Build(request.Phone).ToList();
        var normalizedPhone = candidates.FirstOrDefault() ?? string.Empty;

        if (candidates.Count == 0)
        {
            return new IdentifyWhatsAppSenderResultDto();
        }

        var customerTotals = await bazaresContext.ClosureCustomerTotals
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(total => total.Customer)
            .Where(total => candidates.Contains(total.Customer.Phone)
                && (total.Status == BzaClosureCustomerTotalStatus.Pending
                    || total.Status == BzaClosureCustomerTotalStatus.Rejected))
            .Select(total => new
            {
                total.Id,
                total.TenantId,
                total.TotalAmount,
                total.UploadToken,
                total.Status,
            })
            .ToListAsync(cancellationToken);

        var isCustomer = customerTotals.Count > 0
            || await bazaresContext.Customers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(customer => candidates.Contains(customer.Phone), cancellationToken);

        var customerTenantIds = customerTotals.Select(total => total.TenantId).Distinct().ToList();
        var customerBazarSettings = customerTenantIds.Count == 0
            ? new Dictionary<string, CustomerBazarContact>()
            : await bazaresContext.BazarSettings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(settings => customerTenantIds.Contains(settings.TenantId))
                .Select(settings => new
                {
                    settings.TenantId,
                    BazarName = settings.BazarName ?? "Bazar",
                    BazarWhatsApp = settings.SalesWhatsApp ?? settings.GeneralWhatsApp,
                })
                .ToDictionaryAsync(
                    settings => settings.TenantId,
                    settings => new CustomerBazarContact(settings.BazarName, settings.BazarWhatsApp),
                    cancellationToken);

        var accounts = customerTotals
            .Select(total =>
            {
                customerBazarSettings.TryGetValue(total.TenantId, out var settings);
                return new CustomerWhatsAppAccountDto(
                    total.Id,
                    total.TenantId,
                    settings?.BazarName ?? "Bazar",
                    total.TotalAmount,
                    total.UploadToken,
                    total.Status,
                    settings?.BazarWhatsApp);
            })
            .ToList();

        return new IdentifyWhatsAppSenderResultDto
        {
            NormalizedPhone = normalizedPhone,
            Role = isCustomer ? WhatsAppSenderRole.Customer : WhatsAppSenderRole.Unknown,
            CustomerAccounts = accounts,
        };
    }

    private sealed record CustomerBazarContact(string BazarName, string? BazarWhatsApp);
}