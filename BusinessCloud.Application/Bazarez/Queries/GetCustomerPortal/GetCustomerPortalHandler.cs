using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerPortal;

public class GetCustomerPortalHandler : IRequestHandler<GetCustomerPortalQuery, CustomerPortalDto>
{
    private readonly IBazaresDbContext _context;

    public GetCustomerPortalHandler(IBazaresDbContext context) => _context = context;

    public async Task<CustomerPortalDto> Handle(GetCustomerPortalQuery request, CancellationToken ct)
    {
        var customer = await _context.Customers
            .IgnoreQueryFilters() // Portal público, no requiere tenant filter por token
            .Include(c => c.Collector)
            .Include(c => c.Sales).ThenInclude(s => s.Products)
            .Include(c => c.Sales).ThenInclude(s => s.Payments)
            .FirstOrDefaultAsync(c => c.PortalToken == request.PortalToken, ct)
            ?? throw new KeyNotFoundException("Token de portal inválido.");

        var statusNames = new Dictionary<int, string>
        {
            { 1, "Pendiente" }, { 2, "Pagado" }, { 3, "Listo para Entrega" },
            { 4, "Entregado a Recolector" }, { 5, "Cancelado" }
        };

        var allSales = customer.Sales
            .OrderByDescending(s => s.CreatedAt)
            .Select(s =>
            {
                var paid = s.Payments.Where(p => p.IsVerified).Sum(p => p.Amount);
                return new CustomerPortalSaleDto
                {
                    SaleId = s.Id,
                    Description = s.Description,
                    Products = s.Products.Select(p => $"{p.Description} - ${p.Price:N2}").ToList(),
                    Total = s.Total,
                    Paid = paid,
                    Remaining = Math.Max(0, s.Total - paid),
                    Status = s.Status,
                    StatusName = statusNames.GetValueOrDefault(s.Status, "Desconocido"),
                    PaymentDeadline = s.PaymentDeadline,
                    CreatedAt = s.CreatedAt
                };
            }).ToList();

        return new CustomerPortalDto
        {
            CustomerName = customer.Name,
            CollectorName = customer.Collector.Name,
            CollectorGroup = customer.Collector.GroupId,
            ActiveSales = allSales.Where(s => s.Status < 4).ToList(),
            History = allSales.Where(s => s.Status >= 4).ToList(),
            TotalPending = allSales.Where(s => s.Status < 4).Sum(s => s.Remaining)
        };
    }
}
