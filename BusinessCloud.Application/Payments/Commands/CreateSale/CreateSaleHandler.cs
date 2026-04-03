
using BusinessCloud.Domain.Payments.Entities;
using BusinessCloud.Infrastructure.Data;
using BusinessCloud.Infrastructure.Persistence;
using MediatR;
using MongoDB.Driver;

namespace BusinessCloud.Application.Payments.Commands.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, int>
{
    private readonly PaymentsDbContext _sqlContext;
    private readonly MongoContext _mongoContext;

    public CreateSaleHandler(PaymentsDbContext sqlContext, MongoContext mongoContext)
    {
        _sqlContext = sqlContext;
        _mongoContext = mongoContext;
    }

    public async Task<int> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        // 1. Cálculo automático de comisión si hay SellerId [cite: 8, 27]
        decimal commissionPercent = request.SellerId.HasValue ? 0.10m : 0m;
        decimal calculatedCommission = request.TotalAmount * commissionPercent;

        // 2. Mapeo a la entidad (El TenantId se asigna solo en el SaveChanges) [cite: 44]
        var sale = new Sale
        {
            CustomerId = request.CustomerId,
            SellerId = request.SellerId,
            TotalAmount = request.TotalAmount,
            CostPrice = request.CostPrice,
            CommissionAmount = calculatedCommission,
            IsCommissionPaid = false,
            Date = DateTime.UtcNow
        };

        // 3. AGREGAR MOVIMIENTO INICIAL (Venta como transacción de cargo)
        // Esto asegura que en la tabla Payments siempre haya un rastro del origen
        var initialMovement = new Payment
        {
            Amount = 0, // Si es crédito puro, o puedes poner el monto del enganche si el comando lo trae
            Date = DateTime.UtcNow,
            Reference = "Registro inicial de venta",
            // Sale se asociará automáticamente al agregarla a la colección si tienes la navegación
        };

        // Si tu entidad Sale tiene ICollection<Payment> Payments:
        sale.Payments = new List<Payment> { initialMovement };

        // 4. Guardar en SQL Server (EF Core envuelve esto en una sola transacción SQL)
        _sqlContext.Sales.Add(sale);
        await _sqlContext.SaveChangesAsync(cancellationToken);

        // 5. Audit Log en MongoDB (Igual que antes)
        var auditCollection = _mongoContext.GetCollection<object>("AuditLogs");
        await auditCollection.InsertOneAsync(new
        {
            Event = "SaleAndInitialMovementCreated",
            SaleId = sale.Id,
            TenantId = sale.TenantId,
            Details = new
            {
                request.TotalAmount,
                InitialPayment = initialMovement.Amount
            },
            CreatedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        return sale.Id;
    }
}