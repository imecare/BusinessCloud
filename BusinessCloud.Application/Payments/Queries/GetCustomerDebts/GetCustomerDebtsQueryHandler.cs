using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerDebts;

public sealed class GetCustomerDebtsQueryHandler(IPaymentsDbContext db)
    : IRequestHandler<GetCustomerDebtsQuery, List<CustomerDebtDto>>
{
    public async Task<List<CustomerDebtDto>> Handle(
        GetCustomerDebtsQuery request,
        CancellationToken cancellationToken)
    {
        var pendingSales = await ActiveDebtSales.Query(db)
            .Select(sale => new
            {
                sale.CustomerId,
                CustomerName = sale.Customer.Name + " " + sale.Customer.LastName,
                SellerId = sale.Customer.SellerId,
                SellerName = sale.Customer.Seller != null
                    ? sale.Customer.Seller.Name + " " + sale.Customer.Seller.LastName : string.Empty,
                sale.TotalAmount,
                PaidAmount = sale.Payment
                    .Where(payment => payment.PaymentTypeId == 2)
                    .Sum(payment => payment.Amount),
                LastPaymentDate = sale.Payment
                    .Where(payment => payment.PaymentTypeId == 2)
                    .Max(payment => (DateTime?)payment.PaymentDate)
            })
            .ToListAsync(cancellationToken);

        return pendingSales
            .Select(sale => new
            {
                Sale = sale,
                DebtAmount = Math.Max(sale.TotalAmount - sale.PaidAmount, 0m)
            })
            .Where(item => item.DebtAmount > 0m)
            .GroupBy(item => new
            {
                item.Sale.CustomerId,
                item.Sale.CustomerName,
                item.Sale.SellerId,
                item.Sale.SellerName
            })
            .Select(group => new CustomerDebtDto(
                group.Key.CustomerId,
                group.Key.CustomerName.Trim(),
                group.Key.SellerId,
                group.Key.SellerName.Trim(),
                group.Sum(item => item.Sale.TotalAmount),
                group.Sum(item => item.Sale.PaidAmount),
                group.Sum(item => item.DebtAmount),
                group.Count(),
                group.Max(item => item.Sale.LastPaymentDate)))
            .OrderBy(debt => debt.LastPaymentDate.HasValue)
            .ThenBy(debt => debt.LastPaymentDate)
            .ThenBy(debt => debt.CustomerName)
            .ToList();
    }
}