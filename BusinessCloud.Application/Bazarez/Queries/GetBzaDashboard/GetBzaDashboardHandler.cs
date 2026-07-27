using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;

public class GetBzaDashboardHandler(
    IBazaresDbContext context,
    IIdentityDbContext identityContext,
    ICurrentUserService currentUser)
    : IRequestHandler<GetBzaDashboardQuery, BzaDashboardDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IIdentityDbContext _identityContext = identityContext;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<BzaDashboardDto> Handle(GetBzaDashboardQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var (periodStart, periodEndExclusive) = ResolvePeriodWindow(request.Period, today);

        var totalCustomers = await _context.Customers.CountAsync(ct);
        var totalCollectors = await _context.Collectors.CountAsync(ct);

        var closureEvents = await _context.ClosureEvents
            .AsNoTracking()
            .ToListAsync(ct);

        var closureTotals = await _context.ClosureCustomerTotals
            .AsNoTracking()
            .Include(t => t.ClosureEvent)
            .Include(t => t.Customer)
                .ThenInclude(c => c.Collector)
                    .ThenInclude(co => co.CollectorGroup)
            .ToListAsync(ct);

        var activeTotals = closureTotals
            .Where(t => t.ClosureEvent.Status != BzaClosureEventStatus.Cancelled
                        && t.Status != BzaClosureCustomerTotalStatus.Cancelled)
            .ToList();

        var periodTotals = activeTotals
            .Where(t => t.ClosureEvent.CreatedAt >= periodStart && t.ClosureEvent.CreatedAt < periodEndExclusive)
            .ToList();

        var totalSent = periodTotals.Sum(t => t.TotalAmount);
        var totalPaid = periodTotals
            .Where(t => t.Status == BzaClosureCustomerTotalStatus.Validated)
            .Sum(t => t.TotalAmount);
        var totalPending = Math.Max(0m, totalSent - totalPaid);

        var pendingTotals = periodTotals
            .Where(t => t.Status == BzaClosureCustomerTotalStatus.Pending
                        || t.Status == BzaClosureCustomerTotalStatus.ProofReceived
                        || t.Status == BzaClosureCustomerTotalStatus.Rejected)
            .ToList();

        var pendingValidationCount = periodTotals.Count(t => t.Status == BzaClosureCustomerTotalStatus.ProofReceived);
        var rejectedProofCount = periodTotals.Count(t => t.Status == BzaClosureCustomerTotalStatus.Rejected);
        var customersWithPendingBalance = pendingTotals.Select(t => t.BzaCustomerId).Distinct().Count();
        var pendingWithdrawalsToValidate = periodTotals.Count(t =>
            t.PaymentMethod == 3 && t.Status == BzaClosureCustomerTotalStatus.ProofReceived);

        var activeClosures = closureEvents.Where(c => c.Status != BzaClosureEventStatus.Cancelled).ToList();
        var periodClosures = activeClosures
            .Where(c => c.CreatedAt >= periodStart && c.CreatedAt < periodEndExclusive)
            .ToList();

        var closuresInDelivery = periodClosures.Count(c => c.InDeliveryProcess && !c.Delivered);
        var finalizedClosures = periodClosures.Count(c => c.Delivered);

        var collectorVolume = periodTotals
            .GroupBy(t => new
            {
                CollectorId = t.Customer?.BzaCollectorId,
                CollectorName = t.Customer?.Collector?.Name,
                GroupId = t.Customer?.Collector?.BzaCollectorGroupId,
                GroupDescription = t.Customer?.Collector?.CollectorGroup?.Description,
            })
            .Select(g => new CollectorVolumeDto
            {
                CollectorId = g.Key.CollectorId ?? 0,
                CollectorName = string.IsNullOrWhiteSpace(g.Key.CollectorName) ? "Sin recolector" : g.Key.CollectorName,
                BzaCollectorGroupId = g.Key.GroupId,
                GroupDescription = g.Key.GroupDescription,
                CustomerCount = g.Select(x => x.BzaCustomerId).Distinct().Count(),
                TotalSales = g.Sum(x => x.TotalAmount),
                TotalCollected = g.Where(x => x.Status == BzaClosureCustomerTotalStatus.Validated).Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.TotalSales)
            .ToList();

        var delinquents = pendingTotals
            .Where(t => t.ClosureEvent.PaymentDeadline.Date < today)
            .GroupBy(t => new
            {
                t.BzaCustomerId,
                CustomerName = t.Customer?.Name ?? "Cliente",
                CustomerPhone = t.Customer?.Phone ?? string.Empty,
            })
            .Select(g => new DelinquentCustomerDto
            {
                CustomerId = g.Key.BzaCustomerId,
                CustomerName = g.Key.CustomerName,
                CustomerPhone = g.Key.CustomerPhone,
                Balance = g.Sum(x => x.TotalAmount),
                PaymentDeadline = g.Min(x => x.ClosureEvent.PaymentDeadline),
                OverdueSales = g.Select(x => x.BzaClosureEventId).Distinct().Count(),
            })
            .OrderByDescending(d => d.Balance)
            .ToList();

        var recoveryRate = totalSent > 0 ? Math.Round((totalPaid / totalSent) * 100m, 2) : 0m;

        var tenantId = _currentUser.TenantId;
        var messagesAvailable = string.IsNullOrEmpty(tenantId)
            ? 0
            : await _identityContext.TenantMessageBalances
                .AsNoTracking()
                .Where(b => b.TenantId == tenantId)
                .Select(b => (int?)b.Available)
                .FirstOrDefaultAsync(ct) ?? 0;

        return new BzaDashboardDto
        {
            TotalCustomers = totalCustomers,
            TotalCollectors = totalCollectors,
            WeeklySales = totalSent,
            TotalSent = totalSent,
            TotalPaid = totalPaid,
            TotalPending = totalPending,
            PendingSales = periodClosures.Count(c => c.Status == BzaClosureEventStatus.PendingPayment || c.Status == BzaClosureEventStatus.ProofReceived),
            PaidSales = periodClosures.Count(c => c.Status == BzaClosureEventStatus.Validated),
            DeliveredSales = finalizedClosures,
            DelinquentsCount = delinquents.Count,
            MessagesAvailable = messagesAvailable,
            PendingValidationCount = pendingValidationCount,
            RejectedProofCount = rejectedProofCount,
            CustomersWithPendingBalance = customersWithPendingBalance,
            PendingWithdrawalsToValidate = pendingWithdrawalsToValidate,
            ClosuresInDelivery = closuresInDelivery,
            FinalizedClosures = finalizedClosures,
            RecoveryRate = recoveryRate,
            CollectorVolumes = collectorVolume,
            Delinquents = delinquents
        };
    }

    private static (DateTime start, DateTime endExclusive) ResolvePeriodWindow(string? period, DateTime today)
    {
        var normalized = (period ?? "week").Trim().ToLowerInvariant();

        if (normalized == "today")
        {
            return (today, today.AddDays(1));
        }

        if (normalized == "month")
        {
            var monthStart = new DateTime(today.Year, today.Month, 1);
            return (monthStart, monthStart.AddMonths(1));
        }

        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        return (weekStart, weekStart.AddDays(7));
    }
}
