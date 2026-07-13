using System.Globalization;
using System.Text;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GenerateChargeMessage;

public class GenerateChargeMessageHandler(IBazaresDbContext context)
    : IRequestHandler<GenerateChargeMessageQuery, ChargeMessageResultDto>
{
    private readonly IBazaresDbContext _context = context;
    private static readonly CultureInfo Culture = new("es-MX");

    public async Task<ChargeMessageResultDto> Handle(GenerateChargeMessageQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // Ventas del cliente (una por evento) con su evento y productos.
        var customerSales = await _context.Sales
            .Include(s => s.Event)
            .Include(s => s.Products)
            .Where(s => s.BzaCustomerId == request.BzaCustomerId)
            .ToListAsync(cancellationToken);

        var eventIds = customerSales.Select(s => s.BzaEventId).Distinct().ToList();

        // Pagos aprobados del cliente en esos eventos.
        var verifiedPayments = await _context.Payments
            .Where(p => eventIds.Contains(p.BzaEventId)
                        && p.BzaCustomerId == request.BzaCustomerId
                        && p.IsVerified)
            .ToListAsync(cancellationToken);

        // Tarjetas activas para incluir como métodos de pago.
        var activeCards = await _context.PaymentCards
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);

        // Texto fijo personalizado del bazar para el mensaje de cobro.
        var settings = await _context.NotificationSettings
            .FirstOrDefaultAsync(cancellationToken);
        var customIntro = settings?.ChargeMessage?.Trim() ?? string.Empty;

        // Calcular pendiente por evento y conservar solo los que tienen saldo.
        var pendingGroups = customerSales
            .Select(s =>
            {
                var subtotal = s.Products.Sum(p => p.Price);
                var paid = verifiedPayments.Where(p => p.BzaEventId == s.BzaEventId).Sum(p => p.Amount);
                var pending = Math.Max(0m, subtotal - paid);
                return new
                {
                    Sale = s,
                    Subtotal = subtotal,
                    Paid = paid,
                    Pending = pending
                };
            })
            .Where(x => x.Pending > 0m)
            .OrderBy(x => x.Sale.Event.CreatedAt)
            .ToList();

        var totalPending = pendingGroups.Sum(x => x.Pending);
        var message = BuildMessage(customer.Name, customIntro, pendingGroups
            .Select(x => (x.Sale, x.Subtotal, x.Paid, x.Pending)).ToList(), totalPending, activeCards);

        return new ChargeMessageResultDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = new string((customer.Phone ?? string.Empty).Where(char.IsDigit).ToArray()),
            TotalPending = totalPending,
            HasPending = totalPending > 0m,
            Message = message
        };
    }

    private static string BuildMessage(
        string customerName,
        string customIntro,
        List<(Domain.Bazares.Entities.BzaSale Sale, decimal Subtotal, decimal Paid, decimal Pending)> groups,
        decimal totalPending,
        List<Domain.Bazares.Entities.BzaPaymentCard> activeCards)
    {
        var sb = new StringBuilder();
        sb.Append("Hola ").Append(customerName).AppendLine(" 👋");

        // Parte fija personalizada por el bazar (si está configurada).
        if (!string.IsNullOrWhiteSpace(customIntro))
        {
            sb.AppendLine();
            sb.AppendLine(customIntro);
        }

        sb.AppendLine();

        if (groups.Count == 0)
        {
            sb.AppendLine("¡Gracias! No tienes saldos pendientes por el momento. 🎉");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine("Te compartimos el detalle de tu cuenta pendiente:");
        sb.AppendLine();

        foreach (var group in groups)
        {
            var saleEvent = group.Sale.Event;
            sb.Append("🛍️ *").Append(saleEvent.Description).Append('*');
            sb.Append(" (").Append(saleEvent.CreatedAt.ToString("dd/MM/yyyy", Culture)).AppendLine(")");

            foreach (var product in group.Sale.Products.OrderBy(p => p.CreatedAt))
            {
                sb.Append("   • ").Append(product.Description)
                  .Append(" — ").AppendLine(Money(product.Price));
            }

            sb.Append("   Subtotal: ").Append(Money(group.Subtotal));
            if (group.Paid > 0m)
            {
                sb.Append("  |  Abonado: ").Append(Money(group.Paid));
            }
            sb.Append("  |  Pendiente: ").AppendLine(Money(group.Pending));
            sb.AppendLine();
        }

        sb.Append("💰 *Total pendiente: ").Append(Money(totalPending)).AppendLine("*");

        if (activeCards.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Puedes realizar tu pago en:");
            foreach (var card in activeCards)
            {
                sb.Append("💳 ").Append(card.CardNumber);
                sb.Append(" — ").Append(card.CardHolderName);
                if (!string.IsNullOrWhiteSpace(card.Bank))
                {
                    sb.Append(" (").Append(card.Bank).Append(')');
                }
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(card.Notes))
                {
                    sb.Append("   ℹ️ ").AppendLine(card.Notes);
                }
            }
        }

        sb.AppendLine();
        sb.Append("¡Gracias por tu compra! 😊");

        return sb.ToString().TrimEnd();
    }

    private static string Money(decimal amount) => amount.ToString("C", Culture);
}
