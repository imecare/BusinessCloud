using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetPendingWithdrawalsReport;

/// <summary>
/// Reporte de retiros sin tarjeta pendientes de validar: totales de cierre cuyo
/// método de pago declarado es "retiro sin tarjeta" (3) y que ya tienen comprobante
/// recibido (Status = 2), a la espera de que el bazar los valide.
/// Incluye cliente, monto, banco declarado, la venta (cierre) asociada y los enlaces
/// a las imágenes de los comprobantes para consultar cada retiro de uno.
/// </summary>
public record GetPendingWithdrawalsReportQuery(DateTime? From = null, DateTime? To = null)
    : IRequest<PendingWithdrawalsReportDto>;

public class PendingWithdrawalsReportDto
{
    public int TotalPending { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Detalle de cada retiro sin tarjeta pendiente de validar.</summary>
    public List<PendingWithdrawalItemDto> Items { get; set; } = new();
}

public record PendingWithdrawalItemDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string? Bank,
    string? Reference,
    string SaleDescription,
    decimal TotalAmount,
    DateTime? ProofUploadedAt,
    List<string> ProofUrls);

public class GetPendingWithdrawalsReportHandler(IBazaresDbContext context)
    : IRequestHandler<GetPendingWithdrawalsReportQuery, PendingWithdrawalsReportDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<PendingWithdrawalsReportDto> Handle(GetPendingWithdrawalsReportQuery request, CancellationToken cancellationToken)
    {
        // Retiro sin tarjeta (PaymentMethod = 3) con comprobante recibido pendiente de validar (Status = 2).
        var query = _context.ClosureCustomerTotals
            .Include(t => t.Customer)
            .Include(t => t.Proofs)
            .Include(t => t.ClosureEvent)
            .Where(t => t.PaymentMethod == 3
                        && t.Status == BzaClosureCustomerTotalStatus.ProofReceived);

        if (request.From.HasValue)
            query = query.Where(t => t.ProofUploadedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(t => t.ProofUploadedAt <= request.To.Value);

        var totals = await query
            .OrderByDescending(t => t.ProofUploadedAt)
            .ToListAsync(cancellationToken);

        var items = totals.Select(t => new PendingWithdrawalItemDto(
            t.Id,
            t.BzaCustomerId,
            t.Customer.Name,
            t.Customer.Phone,
            t.WithdrawalBank,
            t.CustomerReference,
            t.ClosureEvent.Description,
            t.TotalAmount,
            t.ProofUploadedAt,
            t.Proofs
                .OrderBy(p => p.UploadedAt)
                .Select(p => p.ImageUrl)
                .ToList()))
            .ToList();

        return new PendingWithdrawalsReportDto
        {
            TotalPending = items.Count,
            TotalAmount = items.Sum(i => i.TotalAmount),
            Items = items
        };
    }
}
