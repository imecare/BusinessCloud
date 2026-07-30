using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.EstimateClosureTransactions;

/// <summary>Estimación de transacciones antes de enviar los totales de un cierre.</summary>
public class ClosureTransactionsEstimateDto
{
    /// <summary>Clientes que consumirán transacción en este envío (aún no cobrados).</summary>
    public int ToSend { get; set; }

    /// <summary>Transacciones disponibles (saldo pagado).</summary>
    public int Available { get; set; }

    /// <summary>Cortesías que aún puede usar.</summary>
    public int CourtesyRemaining { get; set; }

    /// <summary>Cortesías que se otorgarían para completar este envío.</summary>
    public int CourtesyToGrant { get; set; }

    /// <summary>true si alcanza (saldo + cortesía) para completar el envío.</summary>
    public bool CanSend { get; set; }

    /// <summary>true si NO alcanza y necesita contratar antes de enviar.</summary>
    public bool NeedsContract { get; set; }
}

public record EstimateClosureTransactionsQuery(
    int ClosureEventId,
    IReadOnlyList<int>? CustomerIds = null) : IRequest<ClosureTransactionsEstimateDto>;

public class EstimateClosureTransactionsHandler(
    IBazaresDbContext context,
    IIdentityDbContext identityContext,
    ICurrentUserService currentUser)
    : IRequestHandler<EstimateClosureTransactionsQuery, ClosureTransactionsEstimateDto>
{
    public async Task<ClosureTransactionsEstimateDto> Handle(EstimateClosureTransactionsQuery request, CancellationToken ct)
    {
        var totals = await context.ClosureCustomerTotals
            .AsNoTracking()
            .Where(t => t.BzaClosureEventId == request.ClosureEventId)
            .Select(t => new { t.BzaCustomerId, t.TransactionCharged })
            .ToListAsync(ct);

        if (request.CustomerIds is { Count: > 0 } ids)
        {
            totals = totals.Where(t => ids.Contains(t.BzaCustomerId)).ToList();
        }

        var toSend = totals.Count(t => !t.TransactionCharged);

        var tenantId = currentUser.TenantId;
        var balance = string.IsNullOrEmpty(tenantId)
            ? null
            : await identityContext.TenantMessageBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TenantId == tenantId, ct);

        var available = balance?.Available ?? 0;
        var courtesyRemaining = System.Math.Max(0, TransactionPolicy.CourtesyLimit - (balance?.CourtesyUsed ?? 0));

        var shortfall = System.Math.Max(0, toSend - available);
        var courtesyToGrant = System.Math.Min(shortfall, courtesyRemaining);
        var canSend = toSend <= available + courtesyRemaining;

        return new ClosureTransactionsEstimateDto
        {
            ToSend = toSend,
            Available = available,
            CourtesyRemaining = courtesyRemaining,
            CourtesyToGrant = canSend ? courtesyToGrant : 0,
            CanSend = canSend,
            NeedsContract = !canSend,
        };
    }
}