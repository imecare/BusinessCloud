using System.Globalization;
using System.Text;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.SendTotals;

public class SendTotalsHandler(IBazaresDbContext context)
    : IRequestHandler<SendTotalsCommand, SendTotalsResultDto>
{
    private readonly IBazaresDbContext _context = context;
    private static readonly CultureInfo Culture = new("es-MX");

    public async Task<SendTotalsResultDto> Handle(SendTotalsCommand request, CancellationToken cancellationToken)
    {
        var eventIds = request.EventIds.Distinct().ToList();

        var events = await _context.Events
            .Where(e => eventIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        if (events.Count != eventIds.Count)
            throw new KeyNotFoundException("Uno o más eventos de venta no existen.");

        var sales = await _context.Sales
            .Include(s => s.Products)
            .Include(s => s.Event)
            .Include(s => s.Customer)
                .ThenInclude(c => c.Collector)
                    .ThenInclude(col => col.CollectorGroup)
            .Where(s => eventIds.Contains(s.BzaEventId) && s.BzaClosureEventId == null)
            .ToListAsync(cancellationToken);

        var verifiedPayments = await _context.Payments
            .Where(p => eventIds.Contains(p.BzaEventId) && p.IsVerified)
            .ToListAsync(cancellationToken);

        var activeCards = await _context.PaymentCards
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var settings = await _context.NotificationSettings.FirstOrDefaultAsync(cancellationToken);
        var customIntro = settings?.ChargeMessage?.Trim() ?? string.Empty;

        var bazarSettings = await _context.BazarSettings.FirstOrDefaultAsync(cancellationToken);
        var bazarName = bazarSettings?.BazarName;

        decimal PendingForSale(BzaSale sale)
        {
            var subtotal = sale.Products.Sum(p => p.Price);
            var paid = verifiedPayments
                .Where(p => p.BzaEventId == sale.BzaEventId && p.BzaCustomerId == sale.BzaCustomerId)
                .Sum(p => p.Amount);
            return Math.Max(0m, subtotal - paid);
        }

        // Mapa de fecha de entrega por grupo según lo capturado en el frontend.
        var deliveryByGroup = request.GroupDeliveries
            .Where(g => g.GroupId > 0)
            .GroupBy(g => g.GroupId)
            .ToDictionary(g => g.Key, g => g.First().DeliveryDate);

        // Descripción automática del cierre.
        var description = BuildClosureDescription(events, request.OfficialDeliveryDate);

        // Actualizar la fecha límite de pago de cada evento incluido.
        // La venta NO se cierra aquí: se cierra cuando el cliente sube su comprobante.
        foreach (var ev in events)
        {
            ev.PaymentDeadline = request.PaymentDeadline;
        }

        var closure = new BzaClosureEvent
        {
            Description = description,
            OfficialDeliveryDate = request.OfficialDeliveryDate,
            PaymentDeadline = request.PaymentDeadline,
            Status = BzaClosureEventStatus.PendingPayment,
            Items = events.Select(e => new BzaClosureEventItem { BzaEventId = e.Id }).ToList(),
            GroupDeliveries = deliveryByGroup
                .Select(kv => new BzaClosureGroupDelivery
                {
                    BzaCollectorGroupId = kv.Key,
                    DeliveryDate = kv.Value
                })
                .ToList()
        };

        var messages = new List<CustomerTotalMessageDto>();

        // Ventas con saldo pendiente que se incluirán en este cierre.
        var pendingSales = sales
            .Select(s => new { Sale = s, Pending = PendingForSale(s) })
            .Where(x => x.Pending > 0m)
            .ToList();

        if (pendingSales.Count == 0)
            throw new InvalidOperationException("Los eventos seleccionados no tienen ventas pendientes por enviar. Es posible que ya estén en un envío de totales.");

        // Agrupar ventas pendientes por cliente.
        var byCustomer = pendingSales
            .GroupBy(x => x.Sale.BzaCustomerId);

        foreach (var customerGroup in byCustomer)
        {
            var firstSale = customerGroup.First().Sale;
            var customer = firstSale.Customer;
            var collectorGroup = customer?.Collector?.CollectorGroup;
            var groupId = collectorGroup?.Id;

            var total = customerGroup.Sum(x => x.Pending);
            DateTime? deliveryDate = groupId.HasValue && deliveryByGroup.TryGetValue(groupId.Value, out var d)
                ? d
                : request.OfficialDeliveryDate;

            var uploadToken = Guid.NewGuid().ToString("N");

            closure.CustomerTotals.Add(new BzaClosureCustomerTotal
            {
                BzaCustomerId = customerGroup.Key,
                BzaCollectorGroupId = groupId,
                TotalAmount = total,
                UploadToken = uploadToken,
                Status = BzaClosureCustomerTotalStatus.Pending
            });

            var message = ClosureMessageBuilder.Build(
                bazarName,
                customer?.Name ?? "Cliente",
                total,
                deliveryDate,
                request.PaymentDeadline);
            messages.Add(new CustomerTotalMessageDto
            {
                CustomerId = customerGroup.Key,
                CustomerName = customer?.Name ?? "Cliente",
                CustomerPhone = new string((customer?.Phone ?? string.Empty).Where(char.IsDigit).ToArray()),
                Total = total,
                UploadToken = uploadToken,
                DeliveryDate = deliveryDate,
                Message = message
            });
        }

        // Marcar las tarjetas activas como enviadas para pago: a partir de ahora
        // no podrán eliminarse ni modificarse, solo activarse/desactivarse.
        foreach (var card in activeCards.Where(c => !c.WasSentForPayment))
        {
            card.WasSentForPayment = true;
        }

        _context.ClosureEvents.Add(closure);
        await _context.SaveChangesAsync(cancellationToken);

        // Vincular las ventas incluidas al cierre: ya no podrán enviarse de nuevo.
        foreach (var x in pendingSales)
        {
            x.Sale.BzaClosureEventId = closure.Id;
        }
        await _context.SaveChangesAsync(cancellationToken);

        return new SendTotalsResultDto
        {
            ClosureEventId = closure.Id,
            Description = description,
            Messages = messages.OrderBy(m => m.CustomerName).ToList()
        };
    }

    private static string BuildClosureDescription(List<BzaEvent> events, DateTime? officialDate)
    {
        var names = string.Join(", ", events.OrderBy(e => e.Id).Select(e => e.Description));
        var sb = new StringBuilder("Cierre: ").Append(names);
        if (officialDate.HasValue)
        {
            sb.Append(" — Entrega ").Append(FormatLongDate(officialDate.Value));
        }
        return sb.ToString();
    }

    private static string FormatLongDate(DateTime date)
    {
        var text = date.ToString("dddd dd 'de' MMMM", Culture);
        return text.Length > 0 ? char.ToUpper(text[0], Culture) + text[1..] : text;
    }
}
