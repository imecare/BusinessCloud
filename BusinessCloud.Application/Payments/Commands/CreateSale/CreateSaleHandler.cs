using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessCloud.Application.Payments.Commands.CreateSale;

public class CreateSaleHandler(
    IPaymentsDbContext db,
    IMongoContext mongo,
    ILogger<CreateSaleHandler> logger) : IRequestHandler<CreateSaleCommand, int>
{
    public async Task<int> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        var result = await db.ExecuteSaleTransactionAsync(async ct =>
        {
            if (key is not null)
            {
                var previous = await db.Sales.Include(s => s.Customer).Include(s => s.Payment)
                    .FirstOrDefaultAsync(s => s.IdempotencyKey == key, ct);
                if (previous is not null)
                {
                    if (!Matches(previous, request))
                        throw new InvalidOperationException("La clave de registro ya corresponde a otra venta. Actualice la lista antes de continuar.");
                    return (Id: previous.Id, Created: false, TenantId: previous.TenantId);
                }
            }

            if (request.SellerId.HasValue &&
                !await db.Sellers.AnyAsync(s => s.Id == request.SellerId &&
                    (!request.IsCashSale || s.StatusId == 1), ct))
                throw new ArgumentException("El vendedor no existe en esta empresa o no esta activo.");

            Customer customer;
            if (request.IsCashSale && request.CustomerId == 0)
            {
                customer = await db.Customers.FirstOrDefaultAsync(c => c.IsGeneralPublic, ct)
                    ?? new Customer { Name = "P\u00fablico en general", IsGeneralPublic = true };
            }
            else
            {
                customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct)
                    ?? throw new ArgumentException("El cliente no existe en esta empresa.");
                if (customer.IsGeneralPublic && !request.IsCashSale)
                    throw new ArgumentException("Publico en general solo admite ventas de contado.");
            }

            var sale = new Sale
            {
                Customer = customer,
                SellerId = request.SellerId,
                TotalAmount = request.TotalAmount,
                CostPrice = request.CostPrice,
                CommissionAmount = request.CommissionAmount,
                ProductDescription = request.ProductDescription,
                IsCommissionPaid = false,
                IsCashSale = request.IsCashSale,
                IsPaid = request.IsCashSale,
                IdempotencyKey = key,
                Date = request.Date,
                Payment = new List<Payment>
                {
                    new()
                    {
                        Amount = request.TotalAmount,
                        PaymentDate = request.Date,
                        Date = DateTime.UtcNow,
                        PaymentTypeId = 1,
                        Reference = "Registro inicial de venta"
                    }
                }
            };
            if (request.IsCashSale)
            {
                sale.Payment.Add(new Payment
                {
                    Amount = request.TotalAmount,
                    PaymentDate = request.Date,
                    Date = DateTime.UtcNow,
                    PaymentTypeId = 2,
                    PaymentMethod = request.PaymentMethod!,
                    Reference = "Cobro de venta de contado"
                });
            }

            db.Sales.Add(sale);
            await db.SaveChangesAsync(ct);
            return (Id: sale.Id, Created: true, TenantId: sale.TenantId);
        }, cancellationToken);

        if (result.Created)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await mongo.InsertAuditLogAsync(new
                    {
                        Event = request.IsCashSale ? "CashSaleAndPaymentCreated" : "SaleAndInitialMovementCreated",
                        SaleId = result.Id,
                        result.TenantId,
                        request.TotalAmount,
                        request.IsCashSale,
                        CreatedAt = DateTime.UtcNow
                    }, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error de auditoria MongoDB para SaleId {SaleId}; venta guardada en SQL.", result.Id);
                }
            });
        }

        return result.Id;
    }

    private static bool Matches(Sale sale, CreateSaleCommand request) =>
        sale.IsCashSale == request.IsCashSale &&
        (sale.CustomerId == request.CustomerId || request.CustomerId == 0 && sale.Customer.IsGeneralPublic) &&
        sale.SellerId == request.SellerId && sale.TotalAmount == request.TotalAmount &&
        sale.CostPrice == request.CostPrice && sale.CommissionAmount == request.CommissionAmount &&
        sale.ProductDescription == request.ProductDescription && sale.Date == request.Date &&
        (!request.IsCashSale || sale.Payment.Any(p => p.PaymentTypeId == 2 && p.PaymentMethod == request.PaymentMethod));
}
