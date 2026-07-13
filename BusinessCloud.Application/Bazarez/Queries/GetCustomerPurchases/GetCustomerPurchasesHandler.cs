using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerPurchases;

public class GetCustomerPurchasesHandler(IBazaresDbContext context)
    : IRequestHandler<GetCustomerPurchasesQuery, CustomerPurchasesDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<CustomerPurchasesDto> Handle(GetCustomerPurchasesQuery request, CancellationToken cancellationToken)
    {
        var statusNames = new Dictionary<int, string>
        {
            { 1, "Abierto" },
            { 2, "Cerrado" },
            { 3, "En Entrega" },
            { 4, "Finalizado" },
            { 5, "Cancelado" }
        };

        var customer = await _context.Customers
            .Include(c => c.Sales).ThenInclude(s => s.Event)
            .Include(c => c.Sales).ThenInclude(s => s.Products)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // Filtrar ventas por fecha del evento (en memoria, ya que tenemos los datos cargados)
        var filteredSales = customer.Sales.AsEnumerable();

        if (request.FromDate.HasValue)
            filteredSales = filteredSales.Where(s => s.Event.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            filteredSales = filteredSales.Where(s => s.Event.CreatedAt <= request.ToDate.Value.AddDays(1).AddSeconds(-1));

        // Cada venta corresponde a un evento (una venta por cliente-evento)
        var salesGrouped = filteredSales
            .OrderByDescending(s => s.Event.CreatedAt)
            .Select(s =>
            {
                var sale = s.Event;
                var customerProducts = s.Products.ToList();
                var customerTotal = customerProducts.Sum(p => p.Price);
                var paidAmount = customer.Payments
                    .Where(pay => pay.BzaEventId == sale.Id && pay.IsVerified)
                    .Sum(pay => pay.Amount);

                return new CustomerSaleDto
                {
                    SaleId = sale.Id,
                    SaleDescription = sale.Description,
                    SaleTotal = customerTotal,
                    Status = sale.Status,
                    StatusName = statusNames.GetValueOrDefault(sale.Status, "Desconocido"),
                    PaymentDeadline = sale.PaymentDeadline,
                    PaidAmount = paidAmount,
                    PendingAmount = Math.Max(0, customerTotal - paidAmount),
                    CreatedAt = sale.CreatedAt,
                    Products = customerProducts.Select(p => new CustomerProductDto
                    {
                        Id = p.Id,
                        Description = p.Description,
                        Price = p.Price
                    }).ToList()
                };
            }).ToList();

        return new CustomerPurchasesDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone ?? string.Empty,
            TotalPurchases = salesGrouped.Sum(s => s.SaleTotal),
            TotalPaid = salesGrouped.Sum(s => s.PaidAmount),
            TotalPending = salesGrouped.Sum(s => s.PendingAmount),
            Sales = salesGrouped
        };
    }
}
