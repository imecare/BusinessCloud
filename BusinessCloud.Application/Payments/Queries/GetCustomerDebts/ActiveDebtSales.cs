using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerDebts;

internal static class ActiveDebtSales
{
    internal static IQueryable<Sale> Query(IPaymentsDbContext db) =>
        db.Sales.AsNoTracking().Where(sale => !sale.IsPaid &&
            sale.TotalAmount > sale.Payment.Where(payment => payment.PaymentTypeId == 2).Sum(payment => payment.Amount));
}
