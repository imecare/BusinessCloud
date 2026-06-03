using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerPortal;

public class GetCustomerPortalHandler(IBazaresDbContext context)
    : IRequestHandler<GetCustomerPortalQuery, CustomerPortalDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<CustomerPortalDto> Handle(GetCustomerPortalQuery request, CancellationToken ct)
    {
        var customer = await _context.Customers
            .IgnoreQueryFilters() // Portal público, no requiere tenant filter por token
            .Include(c => c.Collector).ThenInclude(c => c.CollectorGroup)
            .Include(c => c.SoldProducts).ThenInclude(p => p.Sale).ThenInclude(s => s.Payments)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.PortalToken == request.PortalToken, ct)
            ?? throw new KeyNotFoundException("Token de portal inválido.");

        var statusNames = new Dictionary<int, string>
        {
            { 1, "Abierto" }, { 2, "Cerrado" }, { 3, "En Entrega" },
            { 4, "Finalizado" }, { 5, "Cancelado" }
        };

        // Agrupar productos vendidos al cliente por evento de venta
        var salesGrouped = customer.SoldProducts
            .GroupBy(p => p.Sale)
            .OrderByDescending(g => g.Key.CreatedAt)
            .Select(g =>
            {
                var sale = g.Key;
                var customerProducts = g.ToList();
                var customerTotal = customerProducts.Sum(p => p.Price);
                var customerPaid = customer.Payments
                    .Where(pay => pay.BzaSaleId == sale.Id && pay.IsVerified)
                    .Sum(pay => pay.Amount);

                return new CustomerPortalSaleDto
                {
                    SaleId = sale.Id,
                    Description = sale.Description,
                    Products = customerProducts.Select(p => $"{p.Description} - ${p.Price:N2}").ToList(),
                    Total = customerTotal,
                    Paid = customerPaid,
                    Remaining = Math.Max(0, customerTotal - customerPaid),
                    Status = sale.Status,
                    StatusName = statusNames.GetValueOrDefault(sale.Status, "Desconocido"),
                    PaymentDeadline = sale.PaymentDeadline,
                    CreatedAt = sale.CreatedAt
                };
            }).ToList();

        return new CustomerPortalDto
        {
            CustomerName = customer.Name,
            CollectorName = customer.Collector.Name,
            CollectorGroup = customer.Collector.CollectorGroup?.Description,
            ActiveSales = salesGrouped.Where(s => s.Status < 4).ToList(),
            History = salesGrouped.Where(s => s.Status >= 4).ToList(),
            TotalPending = salesGrouped.Where(s => s.Status < 4).Sum(s => s.Remaining)
        };
    }
}
