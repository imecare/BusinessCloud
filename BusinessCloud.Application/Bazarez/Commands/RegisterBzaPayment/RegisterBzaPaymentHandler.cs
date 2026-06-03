using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.RegisterBzaPayment;

public class RegisterBzaPaymentHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<RegisterBzaPaymentCommand, BzaPaymentResultDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<BzaPaymentResultDto> Handle(RegisterBzaPaymentCommand request, CancellationToken ct)
    {
        // 1. Validar que el Evento de Venta exista
        var saleEvent = await _context.Sales.FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, ct)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        if (saleEvent.Status == 5)
            throw new InvalidOperationException("No se puede registrar pago en un evento cancelado.");

        // 2. Validar que el Cliente exista
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 3. Calcular totales del cliente en este evento
        var customerProductsTotal = await _context.SoldProducts
            .Where(p => p.BzaSaleId == request.BzaSaleId && p.BzaCustomerId == request.BzaCustomerId)
            .SumAsync(p => p.Price, ct);

        var customerPaidAmount = await _context.Payments
            .Where(p => p.BzaSaleId == request.BzaSaleId && p.BzaCustomerId == request.BzaCustomerId && p.IsVerified)
            .SumAsync(p => p.Amount, ct);

        // 4. Crear el pago
        var payment = new BzaPayment
        {
            BzaSaleId = request.BzaSaleId,
            BzaCustomerId = request.BzaCustomerId,
            Amount = request.Amount,
            Date = DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            PaymentStatus = 2, // Aprobado directamente (sin comprobante)
            IsVerified = true
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        // 5. Calcular nuevo saldo
        var newTotalPaid = customerPaidAmount + request.Amount;
        var pendingBalance = Math.Max(0, customerProductsTotal - newTotalPaid);
        var isFullyPaid = pendingBalance <= 0;

        // 6. Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_PaymentRegistered",
            SaleEventId = saleEvent.Id,
            SaleEventDescription = saleEvent.Description,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            CustomerTotalInEvent = customerProductsTotal,
            CustomerPaidInEvent = newTotalPaid,
            CustomerPendingInEvent = pendingBalance,
            IsFullyPaid = isFullyPaid,
            Timestamp = DateTime.UtcNow
        }, ct);

        return new BzaPaymentResultDto
        {
            PaymentId = payment.Id,
            CustomerPendingBalanceInEvent = pendingBalance,
            IsFullyPaid = isFullyPaid
        };
    }
}
