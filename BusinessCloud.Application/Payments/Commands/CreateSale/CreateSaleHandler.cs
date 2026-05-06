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
        var sale = new Sale
        {
            CustomerId = request.CustomerId,
            SellerId = request.SellerId,
            TotalAmount = request.TotalAmount,
            CostPrice = request.CostPrice,
            CommissionAmount = request.CommissionAmount,
            ProductDescription = request.ProductDescription,
            IsCommissionPaid = false,
            Date = DateTime.UtcNow
        };

        var initialMovement = new Payment
        {
            Amount = request.TotalAmount,
            Date = DateTime.UtcNow,
            PaymentTypeId = 1,
            Reference = "Registro inicial de venta"
        };

        sale.Payment = new List<Payment> { initialMovement };

        _sqlContext.Sales.Add(sale);
        await _sqlContext.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "SaleAndInitialMovementCreated",
            SaleId = sale.Id,
            TenantId = sale.TenantId,
            Details = new
            {
                request.TotalAmount,
                request.ProductDescription,
                InitialPayment = initialMovement.Amount
            },
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return sale.Id;
    }
}
