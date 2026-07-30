using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaTransactionsBalance;

/// <summary>Resumen del saldo de transacciones (envío de totales) de la empresa.</summary>
public class TransactionsBalanceDto
{
    /// <summary>
    /// Transacciones disponibles a mostrar. Cuando se agota el saldo pagado y se consume
    /// la cortesía, este valor es negativo (lo que se va debiendo de la cortesía).
    /// </summary>
    public int Available { get; set; }

    /// <summary>Cortesías ya consumidas.</summary>
    public int CourtesyUsed { get; set; }

    /// <summary>Límite total de cortesías por empresa.</summary>
    public int CourtesyLimit { get; set; }

    /// <summary>Cortesías que aún puede usar.</summary>
    public int CourtesyRemaining { get; set; }

    /// <summary>Umbral para avisar saldo bajo.</summary>
    public int LowThreshold { get; set; }

    /// <summary>true cuando no hay saldo pagado ni cortesía: no puede procesar totales.</summary>
    public bool Blocked { get; set; }
}

public record GetBzaTransactionsBalanceQuery : IRequest<TransactionsBalanceDto>;

public class GetBzaTransactionsBalanceHandler(
    IIdentityDbContext identityContext,
    ICurrentUserService currentUser)
    : IRequestHandler<GetBzaTransactionsBalanceQuery, TransactionsBalanceDto>
{
    public async Task<TransactionsBalanceDto> Handle(GetBzaTransactionsBalanceQuery request, CancellationToken ct)
    {
        var tenantId = currentUser.TenantId;
        var balance = string.IsNullOrEmpty(tenantId)
            ? null
            : await identityContext.TenantMessageBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TenantId == tenantId, ct);

        var paid = balance?.Available ?? 0;
        var courtesyUsed = balance?.CourtesyUsed ?? 0;
        var courtesyRemaining = System.Math.Max(0, TransactionPolicy.CourtesyLimit - courtesyUsed);

        return new TransactionsBalanceDto
        {
            // Saldo pagado menos cortesía consumida: negativo mientras se usa la cortesía.
            Available = paid - courtesyUsed,
            CourtesyUsed = courtesyUsed,
            CourtesyLimit = TransactionPolicy.CourtesyLimit,
            CourtesyRemaining = courtesyRemaining,
            LowThreshold = TransactionPolicy.LowBalanceThreshold,
            Blocked = paid <= 0 && courtesyRemaining <= 0,
        };
    }
}