using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleCustomers;

public class GetBzaSaleCustomersHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaSaleCustomersQuery, BzaSaleCustomersDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<BzaSaleCustomersDto> Handle(GetBzaSaleCustomersQuery request, CancellationToken cancellationToken)
    {
        var saleEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == request.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // Ventas del evento (una por cliente) con sus productos y datos del cliente/grupo.
        var sales = await _context.Sales
            .Include(s => s.Products)
            .Include(s => s.Customer).ThenInclude(c => c.Collector).ThenInclude(col => col.CollectorGroup)
            .Where(s => s.BzaEventId == request.SaleId)
            .ToListAsync(cancellationToken);

        // Abonos aprobados (PaymentStatus == 2) del evento, agrupados por cliente.
        var paidByCustomer = await _context.Payments
            .Where(p => p.BzaEventId == request.SaleId && p.PaymentStatus == 2)
            .GroupBy(p => p.BzaCustomerId)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken);

        var paidLookup = paidByCustomer.ToDictionary(x => x.CustomerId, x => x.Total);

        var customers = sales
            .Select(s =>
            {
                var totalPurchases = s.Products.Sum(p => p.Price);
                var totalPaid = paidLookup.GetValueOrDefault(s.BzaCustomerId, 0m);
                var balance = totalPurchases - totalPaid;

                return new BzaSaleCustomerItemDto
                {
                    CustomerId = s.BzaCustomerId,
                    CustomerName = s.Customer.Name,
                    CustomerPhone = s.Customer.Phone ?? string.Empty,
                    FacebookName = s.Customer.FacebookName,
                    TotalPurchases = totalPurchases,
                    TotalPaid = totalPaid,
                    Balance = balance,
                    ProductCount = s.Products.Count,
                    IsFullyPaid = totalPurchases > 0 && balance <= 0,
                    CollectorGroupId = s.Customer.Collector?.BzaCollectorGroupId,
                    CollectorGroupName = s.Customer.Collector?.CollectorGroup?.Description
                };
            })
            .OrderBy(c => c.CustomerName)
            .ToList();

        return new BzaSaleCustomersDto
        {
            SaleId = saleEvent.Id,
            SaleDescription = saleEvent.Description,
            Customers = customers,
            TotalCustomers = customers.Count,
            FullyPaidCount = customers.Count(c => c.IsFullyPaid),
            PendingCount = customers.Count(c => !c.IsFullyPaid)
        };
    }
}
