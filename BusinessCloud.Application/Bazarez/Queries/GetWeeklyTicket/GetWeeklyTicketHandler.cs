using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetWeeklyTicket;

public class GetWeeklyTicketHandler(IBazaresDbContext context)
    : IRequestHandler<GetWeeklyTicketQuery, WeeklyTicketDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<WeeklyTicketDto> Handle(GetWeeklyTicketQuery request, CancellationToken ct)
    {
        var customer = await _context.Customers
            .Include(c => c.Collector)
            .FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        // Obtener productos vendidos al cliente esta semana
        var customerProducts = await _context.SoldProducts
            .Include(p => p.Sale)
            .Where(p => p.BzaCustomerId == request.BzaCustomerId
                     && p.CreatedAt >= weekStart
                     && p.CreatedAt < weekEnd)
            .ToListAsync(ct);

        // Agrupar por evento de venta
        var saleEventIds = customerProducts.Select(p => p.BzaSaleId).Distinct().ToList();

        // Obtener pagos del cliente en esos eventos
        var customerPayments = await _context.Payments
            .Where(p => saleEventIds.Contains(p.BzaSaleId) && p.BzaCustomerId == request.BzaCustomerId)
            .ToListAsync(ct);

        var events = customerProducts
            .GroupBy(p => p.Sale)
            .Select(g =>
            {
                var saleEvent = g.Key;
                var products = g.ToList();
                var subtotal = products.Sum(p => p.Price);
                var paid = customerPayments
                    .Where(pay => pay.BzaSaleId == saleEvent.Id && pay.IsVerified)
                    .Sum(pay => pay.Amount);

                return new WeeklyEventItemDto
                {
                    SaleEventId = saleEvent.Id,
                    EventDescription = saleEvent.Description,
                    PaymentDeadline = saleEvent.PaymentDeadline,
                    DeliveryDate = saleEvent.DeliveryDate,
                    Products = products.Select(p => new WeeklyProductDto
                    {
                        Id = p.Id,
                        Description = p.Description,
                        Price = p.Price
                    }).ToList(),
                    Subtotal = subtotal,
                    Paid = paid,
                    Pending = Math.Max(0, subtotal - paid)
                };
            }).ToList();

        var grandTotal = events.Sum(e => e.Subtotal);
        var totalPaid = events.Sum(e => e.Paid);

        return new WeeklyTicketDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Phone = customer.Phone,
            CollectorName = customer.Collector.Name,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            EarliestPaymentDeadline = events
                .Where(e => e.PaymentDeadline.HasValue)
                .Min(e => e.PaymentDeadline),
            Events = events,
            GrandTotal = grandTotal,
            TotalPaid = totalPaid,
            RemainingBalance = Math.Max(0, grandTotal - totalPaid)
        };
    }
}
