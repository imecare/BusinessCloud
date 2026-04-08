using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;


namespace BusinessCloud.Application.Payments.Commands.CreateSale;

public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, int>
{
    private readonly IPaymentsDbContext _sqlContext;
    private readonly IMongoContext _mongoContext;

    public CreateSaleHandler(IPaymentsDbContext sqlContext, IMongoContext mongoContext)
    {
        _sqlContext = sqlContext;
        _mongoContext = mongoContext;
    }

    public async Task<int> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        // 1. Cálculo automático de comisión si hay SellerId
        decimal commissionPercent = request.SellerId.HasValue ? 0.10m : 0m;
        decimal calculatedCommission = request.TotalAmount * commissionPercent;

        // 2. Mapeo a la entidad
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

        // 3. Movimiento inicial
        var initialMovement = new Payment
        {
            Amount = request.TotalAmount,
            Date = DateTime.UtcNow,
            PaymentTypeId =1,
            Reference = "Registro inicial de venta"
        };

        sale.Payments = new List<Payment> { initialMovement };

        // 4. Guardar en SQL
        _sqlContext.Sales.Add(sale);
        await _sqlContext.SaveChangesAsync(cancellationToken);

        // 5. Audit Log en MongoDB usando la abstracción del contexto (no GetCollection)
        await _mongoContext.InsertAuditLogAsync(new
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
        }, cancellationToken);

        return sale.Id;
    }
}