using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetCustomerEventTicket;

public class GetCustomerEventTicketHandler(IBazaresDbContext context)
    : IRequestHandler<GetCustomerEventTicketQuery, CustomerEventTicketDto>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> PaymentStatusNames = new()
    {
        { 1, "Preautorizado" },
        { 2, "Aprobado" },
        { 3, "Rechazado" }
    };

    public async Task<CustomerEventTicketDto> Handle(GetCustomerEventTicketQuery request, CancellationToken cancellationToken)
    {
        // 1. Validar que el cliente exista
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 2. Validar que el evento de venta exista
        var saleEvent = await _context.Sales
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // 3. Obtener productos vendidos al cliente en este evento
        var products = await _context.SoldProducts
            .Where(p => p.BzaSaleId == request.SaleId && p.BzaCustomerId == request.CustomerId)
            .Select(p => new TicketProductDto
            {
                Id = p.Id,
                Description = p.Description,
                Price = p.Price
            })
            .ToListAsync(cancellationToken);

        // 4. Obtener pagos del cliente en este evento
        var payments = await _context.Payments
            .Where(p => p.BzaSaleId == request.SaleId && p.BzaCustomerId == request.CustomerId)
            .OrderByDescending(p => p.Date)
            .Select(p => new TicketPaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Date = p.Date,
                PaymentMethod = p.PaymentMethod,
                Reference = p.Reference,
                PaymentStatus = p.PaymentStatus,
                PaymentStatusName = PaymentStatusNames.ContainsKey(p.PaymentStatus)
                    ? PaymentStatusNames[p.PaymentStatus]
                    : "Desconocido"
            })
            .ToListAsync(cancellationToken);

        // 5. Calcular totales
        var subtotal = products.Sum(p => p.Price);
        var totalPaid = payments.Where(p => p.PaymentStatus == 2).Sum(p => p.Amount); // Solo aprobados
        var pendingAmount = Math.Max(0, subtotal - totalPaid);

        return new CustomerEventTicketDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            SaleEventId = saleEvent.Id,
            EventDescription = saleEvent.Description,
            PaymentDeadline = saleEvent.PaymentDeadline,
            DeliveryDate = saleEvent.DeliveryDate,
            Products = products,
            Subtotal = subtotal,
            TotalPaid = totalPaid,
            PendingAmount = pendingAmount,
            IsPaid = pendingAmount <= 0,
            Payments = payments
        };
    }
}
