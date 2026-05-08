using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.RegisterBzaPayment;

public class RegisterBzaPaymentHandler : IRequestHandler<RegisterBzaPaymentCommand, BzaPaymentResultDto>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public RegisterBzaPaymentHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<BzaPaymentResultDto> Handle(RegisterBzaPaymentCommand request, CancellationToken ct)
    {
        var sale = await _context.Sales
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, ct)
            ?? throw new KeyNotFoundException("Venta no encontrada.");

        if (sale.Status == 5)
            throw new InvalidOperationException("No se puede registrar pago en una venta cancelada.");

        var payment = new BzaPayment
        {
            BzaSaleId = sale.Id,
            Amount = request.Amount,
            Date = DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod,
            ProofImageUrl = request.ProofImageUrl,
            Reference = request.Reference,
            IsVerified = false // Requiere verificación del bazar
        };

        _context.Payments.Add(payment);

        var totalPaid = sale.Payments.Where(p => p.IsVerified).Sum(p => p.Amount) + request.Amount;
        var fullyPaid = totalPaid >= sale.Total;

        if (fullyPaid && sale.Status == 1)
        {
            sale.Status = 2; // Pagado
        }

        await _context.SaveChangesAsync(ct);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_PaymentRegistered",
            SaleId = sale.Id,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Timestamp = DateTime.UtcNow
        }, ct);

        return new BzaPaymentResultDto
        {
            PaymentId = payment.Id,
            NewBalance = Math.Max(0, sale.Total - totalPaid),
            SaleFullyPaid = fullyPaid
        };
    }
}
