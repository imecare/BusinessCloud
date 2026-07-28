using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
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

        // Obtener ventas del cliente (una por evento) con sus productos.
        var customerSales = await _context.Sales
            .Include(s => s.Event)
            .Include(s => s.Products)
            .Where(s => s.BzaCustomerId == request.BzaCustomerId)
            .ToListAsync(cancellationToken);

        var saleEventIds = customerSales.Select(s => s.BzaEventId).Distinct().ToList();

        // Obtener pagos del cliente en esos eventos.
        var customerPayments = await _context.Payments
            .Where(p => saleEventIds.Contains(p.BzaEventId) && p.BzaCustomerId == request.BzaCustomerId)
            .OrderByDescending(p => p.Date)
            .ToListAsync(cancellationToken);

        // El cierre (evento de pago) al que pertenece cada venta se determina directamente
        // desde BzaSale.BzaClosureEventId (fuente de verdad), no desde BzaClosureEventItem.
        // Un mismo BzaEventId puede tener varios BzaClosureEventItem historicos (p.ej. tras
        // mover pendientes a otro cierre), por lo que agrupar por BzaEventId causaria claves
        // duplicadas.
        var closureEventIds = customerSales
            .Where(s => s.BzaClosureEventId.HasValue)
            .Select(s => s.BzaClosureEventId!.Value)
            .Distinct()
            .ToList();

        var closureEvents = await _context.ClosureEvents
            .Include(c => c.DeliveryProofs)
            .Where(c => closureEventIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var closureTotalsByEvent = await _context.ClosureCustomerTotals
            .Where(t => closureEventIds.Contains(t.BzaClosureEventId) && t.BzaCustomerId == request.BzaCustomerId)
            .Select(t => new { t.BzaClosureEventId, t.Status, t.BzaCollectorGroupId })
            .ToDictionaryAsync(t => t.BzaClosureEventId, cancellationToken);

        var eventsGroups = customerSales
            .OrderByDescending(s => s.Event.CreatedAt)
            .Select(s =>
            {
                var saleEvent = s.Event;
                var products = s.Products.OrderByDescending(p => p.CreatedAt).ToList();
                var payments = customerPayments.Where(pay => pay.BzaEventId == saleEvent.Id).ToList();
                var subtotal = products.Sum(p => p.Price);

                var paidAmount = payments.Where(p => p.IsVerified).Sum(p => p.Amount);
                var pendingAmount = Math.Max(0, subtotal - paidAmount);

                // Estado de pago base desde pagos registrados.
                var hasPendingProof = payments.Any(p => !p.IsVerified && p.PaymentStatus == 1);
                var paymentState = pendingAmount <= 0 ? 0 : (hasPendingProof ? 2 : 1);
                var paymentStateName = paymentState switch
                {
                    0 => "Pagado",
                    2 => "Pendiente de validar comprobante",
                    _ => "Pendiente de pago"
                };

                // Estado del evento en historial: debe seguir el flujo real de cierre/entrega
                // cuando ya existe un cierre para ese evento.
                var eventStatus = saleEvent.Status;

                // Resolver comprobante de entrega por grupo (si el cierre ya fue entregado).
                var delivered = false;
                string? deliveryProofUrl = null;

                if (s.BzaClosureEventId.HasValue
                    && closureEvents.TryGetValue(s.BzaClosureEventId.Value, out var closureEvent))
                {
                    // Estado del historial alineado al flujo operativo: Abierto, En Entrega, Finalizado o Cancelado.
                    if (closureEvent.Status == BzaClosureEventStatus.Cancelled)
                    {
                        eventStatus = 5; // Cancelado
                    }
                    else if (closureEvent.Delivered)
                    {
                        eventStatus = 4; // Finalizado
                    }
                    else if (closureEvent.InDeliveryProcess)
                    {
                        eventStatus = 3; // En Entrega
                    }
                    else
                    {
                        eventStatus = 1; // Abierto (a�n no entra a entrega)
                    }

                    if (closureTotalsByEvent.TryGetValue(s.BzaClosureEventId.Value, out var closureTotal))
                    {
                        // Estado de pago debe venir del total del cierre del cliente.
                        if (closureTotal.Status == BzaClosureCustomerTotalStatus.Validated)
                        {
                            paidAmount = subtotal;
                            pendingAmount = 0;
                            paymentState = 0;
                            paymentStateName = "Pagado";
                        }
                        else if (closureTotal.Status == BzaClosureCustomerTotalStatus.ProofReceived)
                        {
                            paymentState = 2;
                            paymentStateName = "Pendiente de validar comprobante";
                        }
                        else
                        {
                            paymentState = pendingAmount <= 0 ? 0 : 1;
                            paymentStateName = paymentState == 0 ? "Pagado" : "Pendiente de pago";
                        }

                        if (closureEvent.Delivered)
                        {
                            delivered = true;

                            deliveryProofUrl = closureTotal.BzaCollectorGroupId.HasValue
                                ? closureEvent.DeliveryProofs
                                    .Where(p => p.BzaCollectorGroupId == closureTotal.BzaCollectorGroupId.Value)
                                    .OrderByDescending(p => p.UploadedAt)
                                    .FirstOrDefault()?.ImageUrl
                                : null;

                            deliveryProofUrl ??= closureEvent.DeliveryProofs
                                .Where(p => p.BzaCollectorGroupId == null)
                                .OrderByDescending(p => p.UploadedAt)
                                .FirstOrDefault()?.ImageUrl;
                        }
                    }
                    else if (closureEvent.Delivered)
                    {
                        delivered = true;

                        deliveryProofUrl = closureEvent.DeliveryProofs
                            .Where(p => p.BzaCollectorGroupId == null)
                            .OrderByDescending(p => p.UploadedAt)
                            .FirstOrDefault()?.ImageUrl;
                    }
                }

                return new EventHistoryGroupDto
                {
                    SaleEventId = saleEvent.Id,
                    EventDescription = saleEvent.Description,
                    CreatedAt = saleEvent.CreatedAt,
                    PaymentDeadline = saleEvent.PaymentDeadline,
                    EventStatus = eventStatus,
                    EventStatusName = EventStatusNames.GetValueOrDefault(eventStatus, "Desconocido"),
                    IsCustomerPaid = pendingAmount <= 0,
                    PaymentState = paymentState,
                    PaymentStateName = paymentStateName,
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
                    }).ToList(),
                    Delivered = delivered,
                    DeliveryProofImageUrl = deliveryProofUrl
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

