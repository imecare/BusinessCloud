using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetWeeklyTicket;

public class GetWeeklyTicketHandler : IRequestHandler<GetWeeklyTicketQuery, WeeklyTicketDto>
{
    private readonly IBazaresDbContext _context;

    public GetWeeklyTicketHandler(IBazaresDbContext context) => _context = context;

    public async Task<WeeklyTicketDto> Handle(GetWeeklyTicketQuery request, CancellationToken ct)
    {
        var customer = await _context.Customers
            .Include(c => c.Collector)
            .FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        var sales = await _context.Sales
            .Include(s => s.Products)
            .Include(s => s.Payments)
            .Where(s => s.BzaCustomerId == request.BzaCustomerId
                     && s.CreatedAt >= weekStart
                     && s.CreatedAt < weekEnd)
            .ToListAsync(ct);

        var statusNames = new Dictionary<int, string>
        {
            { 1, "Pendiente" }, { 2, "Pagado" }, { 3, "Listo para Entrega" },
            { 4, "Entregado a Recolector" }, { 5, "Cancelado" }
        };

        var paidAmount = sales
            .SelectMany(s => s.Payments)
            .Where(p => p.IsVerified)
            .Sum(p => p.Amount);

        var grandTotal = sales.Where(s => s.Status != 5).Sum(s => s.Total);

        return new WeeklyTicketDto
        {
            CustomerName = customer.Name,
            Phone = customer.Phone,
            CollectorName = customer.Collector.Name,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            PaymentDeadline = sales.Where(s => s.PaymentDeadline.HasValue).Min(s => s.PaymentDeadline),
            GrandTotal = grandTotal,
            PaidAmount = paidAmount,
            RemainingBalance = Math.Max(0, grandTotal - paidAmount),
            Items = sales.Select(s => new WeeklyTicketItemDto
            {
                SaleId = s.Id,
                Description = s.Description,
                Products = s.Products.Select(p => $"{p.Description} - ${p.Price:N2}").ToList(),
                Total = s.Total,
                Status = s.Status,
                StatusName = statusNames.GetValueOrDefault(s.Status, "Desconocido")
            }).ToList()
        };
    }
}
