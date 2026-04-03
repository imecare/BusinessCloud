using BusinessCloud.Application.Common.Interfaces; // Para ICacheService
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory; // Para CustomerHistoryDto y PaymentLineDto
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Collections.Generic;

namespace BusinessCloud.Application.Payments.Commands.RegisterPayment;

  public class RegisterPaymentHandler : IRequestHandler<RegisterPaymentCommand, PaymentReceiptDto>
    {
        private readonly PaymentsDbContext _sqlContext;
    private readonly MongoContext _mongoContext;
    private readonly ICacheService _cache; // Inyectamos el caché

    public RegisterPaymentHandler(
        PaymentsDbContext sqlContext,
        MongoContext mongoContext,
        ICacheService cache)
    {
        _sqlContext = sqlContext;
        _mongoContext = mongoContext;
        _cache = cache;
    }

    public async Task<PaymentReceiptDto> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Verificar la venta e incluir al cliente para obtener su teléfono (necesario para el caché)
        var sale = await _sqlContext.Sales
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken);

        if (sale == null) throw new Exception("Venta no encontrada.");

        // 2. Crear el registro del Abono en SQL
        var payment = new Payment
        {
            SaleId = request.SaleId,
            Amount = request.Amount,
            Date = DateTime.UtcNow,
            Reference = request.Reference // Asegúrate de que tu Command tenga Reference
        };

        _sqlContext.Payments.Add(payment);
        await _sqlContext.SaveChangesAsync(cancellationToken);

        // 3. Generar Audit Log en MongoDB
        var auditCollection = _mongoContext.GetCollection<object>("AuditLogs");
        await auditCollection.InsertOneAsync(new
        {
            Event = "PaymentRegistered",
            PaymentId = payment.Id,
            SaleId = sale.Id,
            TenantId = sale.TenantId,
            Amount = request.Amount,
            Timestamp = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        // 4. ACTUALIZAR EL READ MODEL EN MONGODB
        // Esto permite que la consulta pública vea el abono sin ir a SQL
        try
        {
            var mongoCollection = _mongoContext.GetCollection<CustomerHistoryDto>("CustomerReadModel");
            var updateFilter = Builders<CustomerHistoryDto>.Filter.Eq(x => x.SaleId, request.SaleId);

            var newMovement = new PaymentLineDto(
                payment.Id,
                DateTime.UtcNow,
                request.Amount,
                request.Reference ?? "Abono registrado"
            );

            var update = Builders<CustomerHistoryDto>.Update
                .Push(x => x.Movements, newMovement) // Agregamos el movimiento a la lista incrustada
                .Inc(x => x.RemainingBalance, -request.Amount); // Restamos del saldo pendiente

            await mongoCollection.UpdateOneAsync(updateFilter, update, cancellationToken: cancellationToken);

            // 5. INVALIDAR CACHÉ EN REDIS
            // Usamos el teléfono del cliente para borrar solo su caché específico
            if (sale.Customer != null)
            {
                string cacheKey = $"history_{sale.TenantId}_{sale.Customer.Phone}";
                await _cache.RemoveAsync(cacheKey);
            }

            // Consultamos el Read Model que acabamos de actualizar para traer el saldo y movimientos
            var historyCollection = _mongoContext.GetCollection<CustomerHistoryDto>("CustomerReadModel");
            var history = await historyCollection
                .Find(x => x.SaleId == request.SaleId)
                .FirstOrDefaultAsync(cancellationToken);

            // Preparamos los últimos 5 movimientos para el ticket
            var last5 = history?.Movements
                .OrderByDescending(m => m.Date)
                .Take(5)
                .ToList() ?? new List<PaymentLineDto>();

            // Devolvemos el objeto completo al Controller -> Front-end
            return new PaymentReceiptDto(
                Folio: $"PAY-{payment.Id}", // Folio dinámico
                CustomerName: sale.Customer?.Name ?? "Cliente General",
                AmountPaid: request.Amount,
                NewBalance: history?.RemainingBalance ?? 0,
                Date: DateTime.UtcNow,
                LastMovements: last5
            );

        }
        catch (Exception ex)
        {
            // LOGUEAR EL ERROR: Aquí usarías Serilog o App Insights
            // "Error actualizando Read Model para SaleId {sale.Id}, pero la transacción SQL fue exitosa."

            // IMPORTANTE: No lanzamos (throw) la excepción. 
            // Devolvemos el ID del pago porque en SQL ya se guardó correctamente.
            return new PaymentReceiptDto(
                 Folio: $"PAY-{payment.Id}", // Tenemos el ID de SQL
                 CustomerName: sale.Customer?.Name ?? "Cliente General",
                 AmountPaid: request.Amount,
                 NewBalance: 0, // No pudimos obtenerlo de MongoDB
                 Date: DateTime.UtcNow,
                 LastMovements: new List<PaymentLineDto>() // Lista vacía por el fallo
             );

        }

    }
}