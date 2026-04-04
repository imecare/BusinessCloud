using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.RegisterPayment;

public class RegisterPaymentHandler : IRequestHandler<RegisterPaymentCommand, PaymentReceiptDto>
{
    private readonly IPaymentsDbContext _sqlContext;
    private readonly IMongoContext _mongoContext; // Ahora usamos la INTERFAZ
    private readonly ICacheService _cache;

    public RegisterPaymentHandler(IPaymentsDbContext sqlContext, IMongoContext mongoContext, ICacheService cache)
    {
        _sqlContext = sqlContext;
        _mongoContext = mongoContext;
        _cache = cache;
    }

    public async Task<PaymentReceiptDto> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var sale = await _sqlContext.Sales
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken);

        if (sale == null) throw new Exception("Venta no encontrada.");

        var payment = new Payment
        {
            SaleId = request.SaleId,
            Amount = request.Amount,
            Date = DateTime.UtcNow,
            Reference = request.Reference
        };

        _sqlContext.Payments.Add(payment);
        await _sqlContext.SaveChangesAsync(cancellationToken); // Agregado el token

        try
        {
            // 3. Audit Log (Abstraído)
            await _mongoContext.InsertAuditLogAsync(new { Event = "Payment", PaymentId = payment.Id, SaleId = sale.Id }, cancellationToken);

            // 4. Actualizar Read Model (Toda la lógica de Builders se movió a Infrastructure)
            await _mongoContext.UpdateCustomerReadModelAsync(request.SaleId, request.Amount, request.Reference ?? "Abono", cancellationToken);

            // 5. Invalidar Caché
            if (sale.Customer != null)
            {
                await _cache.RemoveAsync($"history_{sale.TenantId}_{sale.Customer.Phone}");
            }

            // Consultar resultado
            var history = await _mongoContext.GetCustomerHistoryAsync(request.SaleId, cancellationToken);

            return new PaymentReceiptDto(
                Folio: $"PAY-{payment.Id}",
                CustomerName: sale.Customer?.Name ?? "Cliente",
                AmountPaid: request.Amount,
                NewBalance: history?.RemainingBalance ?? 0,
                Date: DateTime.UtcNow,
                LastMovements: history?.Movements.OrderByDescending(m => m.Date).Take(5).ToList() ?? new List<PaymentLineDto>()
            );
        }
        catch
        {
            return new PaymentReceiptDto($"PAY-{payment.Id}", sale.Customer?.Name ?? "Cliente", request.Amount, 0, DateTime.UtcNow, new List<PaymentLineDto>());
        }
    }
}