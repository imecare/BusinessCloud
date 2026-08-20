using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.PrepareTotals;

public class PrepareTotalsHandler(IBazaresDbContext context)
    : IRequestHandler<PrepareTotalsQuery, PrepareTotalsResultDto>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<PrepareTotalsResultDto> Handle(PrepareTotalsQuery request, CancellationToken cancellationToken)
    {
        if (request.EventIds is null || request.EventIds.Count == 0)
            throw new ArgumentException("Debes seleccionar al menos un evento de venta.");

        var eventIds = request.EventIds.Distinct().ToList();

        var events = await _context.Events
            .Where(e => eventIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        if (events.Count != eventIds.Count)
            throw new KeyNotFoundException("Uno o más eventos de venta no existen.");

        // Ventas de los eventos con cliente, grupo y productos.
        // Se excluyen las ventas que ya pertenecen a un evento de pago (cierre).
        var sales = await _context.Sales
            .Include(s => s.Products)
            .Include(s => s.Customer)
                .ThenInclude(c => c.Collector)
                    .ThenInclude(col => col.CollectorGroup)
            .Where(s => eventIds.Contains(s.BzaEventId) && s.BzaClosureEventId == null)
            .ToListAsync(cancellationToken);

        // Pagos aprobados en esos eventos.
        var verifiedPayments = await _context.Payments
            .Where(p => eventIds.Contains(p.BzaEventId) && p.IsVerified)
            .ToListAsync(cancellationToken);

        decimal PendingForSale(Domain.Bazares.Entities.BzaSale sale)
        {
            var subtotal = sale.Products.Sum(p => p.Price);
            var paid = verifiedPayments
                .Where(p => p.BzaEventId == sale.BzaEventId && p.BzaCustomerId == sale.BzaCustomerId)
                .Sum(p => p.Amount);
            return Math.Max(0m, subtotal - paid);
        }

        // Solo ventas con saldo pendiente.
        var pendingSales = sales
            .Select(s => new { Sale = s, Pending = PendingForSale(s) })
            .Where(x => x.Pending > 0m)
            .ToList();

        bool HasMinimumCustomerInfo(BzaCustomer customer)
        {
            var collectorKey = CollectorCatalogNameNormalizer.ToComparisonKey(customer.Collector?.Name);
            var hasRealCollector = collectorKey.Length > 0
                && collectorKey != "AUN SIN RECOLECTOR"
                && collectorKey != "SIN ASIGNAR";
            var hasRealPhone = !customer.HasNoWhatsApp
                && !string.IsNullOrWhiteSpace(customer.Phone)
                && !NoWhatsAppNumber.IsPlaceholder(customer.Phone);
            var hasFacebook = !string.IsNullOrWhiteSpace(customer.FacebookName);
            return hasRealCollector && (hasRealPhone || hasFacebook);
        }

        static string DisplayName(BzaCustomer customer)
        {
            var name = (customer.Name ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(name) ? $"Cliente #{customer.Id}" : name;
        }

        // Detalle por evento.
        var eventDtos = events
            .Select(e =>
            {
                var forEvent = pendingSales.Where(x => x.Sale.BzaEventId == e.Id).ToList();
                return new TotalsEventDto(
                    e.Id,
                    e.Description,
                    forEvent.Sum(x => x.Pending),
                    forEvent.Select(x => x.Sale.BzaCustomerId).Distinct().Count());
            })
            .ToList();

        var today = DateTime.Today;

        // Agrupar por grupo de recolección del cliente.
        var groupDtos = pendingSales
            .GroupBy(x =>
            {
                var group = x.Sale.Customer?.Collector?.CollectorGroup;
                return new
                {
                    GroupId = group?.Id ?? 0,
                    GroupName = group?.Description ?? "Sin grupo",
                    DeliveryDay = group?.DeliveryDay ?? BzaCollectorGroup.DefaultDeliveryDay,
                    DeliveryFrequency = group?.DeliveryFrequency
                };
            })
            .Select(g => new TotalsGroupDto(
                g.Key.GroupId,
                g.Key.GroupName,
                g.Key.DeliveryDay,
                g.Key.DeliveryFrequency,
                NextDeliveryDate(g.Key.DeliveryDay, today),
                g.Select(x => x.Sale.BzaCustomerId).Distinct().Count(),
                g.Sum(x => x.Pending)))
            .OrderBy(g => g.GroupName)
            .ToList();

        var distinctCustomers = pendingSales.Select(x => x.Sale.BzaCustomerId).Distinct().Count();
        var totalAmount = pendingSales.Sum(x => x.Pending);

        // Clientes con información incompleta (alta rápida) incluidos en la selección.
        var pendingInfoCustomers = pendingSales
            .Select(x => x.Sale.Customer)
            .Where(c => c is not null && c.IsPendingInfo && !HasMinimumCustomerInfo(c))
            .DistinctBy(c => c!.Id)
            .Select(c => new PendingInfoCustomerDto(
                c!.Id,
                DisplayName(c),
                c.Phone ?? string.Empty,
                c.FacebookName,
                c.Collector?.Name,
                c.HasNoWhatsApp))
            .OrderBy(c => c.Name)
            .ToList();

        var settings = await _context.BazarSettings.FirstOrDefaultAsync(cancellationToken);

        return new PrepareTotalsResultDto
        {
            Events = eventDtos,
            Groups = groupDtos,
            CustomerCount = distinctCustomers,
            TotalAmount = totalAmount,
            SuggestedPaymentDeadline = today.AddDays(7),
            PaymentCutoffTime = settings?.PaymentCutoffTime,
            PendingInfoCustomers = pendingInfoCustomers
        };
    }

    /// <summary>
    /// Calcula la pr�xima fecha de entrega seg�n el d�a configurado del grupo.
    /// Si el d�a configurado es hoy, sugiere el mismo d�a de la siguiente semana.
    /// Si el grupo no tiene d�a configurado, sugiere dentro de 7 d�as.
    /// </summary>
    private static DateTime NextDeliveryDate(int? deliveryDay, DateTime today)
    {
        if (deliveryDay is null)
            return today.AddDays(7);

        var target = (DayOfWeek)deliveryDay.Value;
        var diff = ((int)target - (int)today.DayOfWeek + 7) % 7;
        if (diff == 0)
            diff = 7;
        return today.AddDays(diff);
    }
}
