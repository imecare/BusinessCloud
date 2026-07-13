using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSalePayments;

public class GetBzaSalePaymentsHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaSalePaymentsQuery, List<BzaSalePaymentItemDto>>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> PaymentStatusNames = new()
    {
        { 1, "Preautorizado" },
        { 2, "Aprobado" },
        { 3, "Rechazado" }
    };

    public async Task<List<BzaSalePaymentItemDto>> Handle(GetBzaSalePaymentsQuery request, CancellationToken cancellationToken)
    {
        var eventExists = await _context.Events
            .AnyAsync(e => e.Id == request.SaleId, cancellationToken);

        if (!eventExists)
            throw new KeyNotFoundException("Evento de Venta no encontrado.");

        var query = _context.Payments
            .Include(p => p.Customer)
            .Where(p => p.BzaEventId == request.SaleId);

        if (request.Status.HasValue)
            query = query.Where(p => p.PaymentStatus == request.Status.Value);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new BzaSalePaymentItemDto
            {
                Id = p.Id,
                BzaSaleId = p.BzaEventId,
                BzaCustomerId = p.BzaCustomerId,
                CustomerName = p.Customer.Name,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                Reference = p.Reference,
                ProofImageUrl = p.ProofImageUrl,
                Status = p.PaymentStatus,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
        {
            payment.StatusName = PaymentStatusNames.GetValueOrDefault(payment.Status, "Desconocido");
        }

        return payments;
    }
}
