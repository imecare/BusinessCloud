using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerSalesHistory;

public class GetCustomerSalesHistoryHandler(IBazaresDbContext context)
    : IRequestHandler<GetCustomerSalesHistoryQuery, CustomerSalesHistoryDto>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> EventStatusNames = new()
    {
        { 1, "Abierto" },
        { 2, "Cerrado" },
        { 3, "En Entrega" },
        { 4, "Finalizado" },
        { 5, "Cancelado" }
    };

    private static readonly Dictionary<int, string> PaymentStatusNames = new()
    {
        { 1, "Preautorizado" },
        { 2, "Aprobado" },
        { 3, "Rechazado" }
    };

    public async Task<CustomerSalesHistoryDto> Handle(GetCustomerSalesHistoryQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // Obtener productos vendidos al cliente agrupados por evento
        var customerProducts = await _context.SoldProducts
            .Include(p => p.Sale)
            .Where(p => p.BzaCustomerId == request.BzaCustomerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var saleEventIds = customerProducts.Select(p => p.BzaSaleId).Distinct().ToList();

        // Obtener pagos del cliente en esos eventos
        var customerPayments = await _context.Payments
            .Where(p => saleEventIds.Contains(p.BzaSaleId) && p.BzaCustomerId == request.BzaCustomerId)
            .OrderByDescending(p => p.Date)
            .ToListAsync(cancellationToken);

        var eventsGroups = customerProducts
            .GroupBy(p => p.Sale)
            .OrderByDescending(g => g.Key.CreatedAt)
            .Select(g =>
            {
                var saleEvent = g.Key;
                var products = g.ToList();
                var payments = customerPayments.Where(pay => pay.BzaSaleId == saleEvent.Id).ToList();
                var subtotal = products.Sum(p => p.Price);
                var paidAmount = payments.Where(p => p.IsVerified).Sum(p => p.Amount);
                var pendingAmount = Math.Max(0, subtotal - paidAmount);

                return new EventHistoryGroupDto
                {
                    SaleEventId = saleEvent.Id,
                    EventDescription = saleEvent.Description,
                    CreatedAt = saleEvent.CreatedAt,
                    PaymentDeadline = saleEvent.PaymentDeadline,
                    DeliveryDate = saleEvent.DeliveryDate,
                    EventStatus = saleEvent.Status,
                    EventStatusName = EventStatusNames.GetValueOrDefault(saleEvent.Status, "Desconocido"),
                    IsCustomerPaid = pendingAmount <= 0,
                    Products = products.Select(p => new EventHistoryProductDto
                    {
                        Id = p.Id,
                        Description = p.Description,
                        Price = p.Price,
                        CreatedAt = p.CreatedAt
                    }).ToList(),
                    Subtotal = subtotal,
                    PaidAmount = paidAmount,
                    PendingAmount = pendingAmount,
                    Payments = payments.Select(p => new EventHistoryPaymentDto
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        Date = p.Date,
                        PaymentMethod = p.PaymentMethod,
                        PaymentStatus = p.PaymentStatus,
                        PaymentStatusName = PaymentStatusNames.GetValueOrDefault(p.PaymentStatus, "Desconocido")
                    }).ToList()
                };
            }).ToList();

        return new CustomerSalesHistoryDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone ?? string.Empty,
            TotalPurchases = eventsGroups.Sum(e => e.Subtotal),
            TotalPaid = eventsGroups.Sum(e => e.PaidAmount),
            TotalPending = eventsGroups.Sum(e => e.PendingAmount),
            TotalEvents = eventsGroups.Count,
            PaidEvents = eventsGroups.Count(e => e.IsCustomerPaid),
            PendingEvents = eventsGroups.Count(e => !e.IsCustomerPaid),
            Events = eventsGroups
        };
    }
}
