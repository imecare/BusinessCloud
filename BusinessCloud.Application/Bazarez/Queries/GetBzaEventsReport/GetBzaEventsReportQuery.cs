using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaEventsReport;

/// <summary>
/// Genera un reporte en Excel de los Eventos de Venta seleccionados: por cada cliente
/// participante muestra su total, productos, estatus de pago, método de pago (si pagó),
/// fecha de entrega y fecha de pago.
/// </summary>
public record GetBzaEventsReportQuery(List<int> EventIds) : IRequest<BzaEventsReportResult>;

public record BzaEventsReportResult(byte[] FileContent, string FileName, string ContentType);

public class GetBzaEventsReportHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaEventsReportQuery, BzaEventsReportResult>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> PaymentMethodNames = new()
    {
        { 0, "No especificado" },
        { 1, "Transferencia" },
        { 2, "Depósito" },
        { 3, "Retiro sin tarjeta" },
    };

    public async Task<BzaEventsReportResult> Handle(GetBzaEventsReportQuery request, CancellationToken cancellationToken)
    {
        var eventIds = (request.EventIds ?? new List<int>()).Distinct().ToList();

        var sales = eventIds.Count == 0
            ? new List<BzaSale>()
            : await _context.Sales
                .Include(s => s.Event)
                .Include(s => s.Customer).ThenInclude(c => c.Collector)
                .Include(s => s.Products)
                .Where(s => eventIds.Contains(s.BzaEventId))
                .ToListAsync(cancellationToken);

        sales = sales
            .OrderBy(s => s.Event.Description)
            .ThenBy(s => s.Customer.Name)
            .ToList();

        var closureEventIds = sales
            .Where(s => s.BzaClosureEventId.HasValue)
            .Select(s => s.BzaClosureEventId!.Value)
            .Distinct()
            .ToList();

        var closureTotals = closureEventIds.Count == 0
            ? new List<BzaClosureCustomerTotal>()
            : await _context.ClosureCustomerTotals
                .AsNoTracking()
                .Where(t => closureEventIds.Contains(t.BzaClosureEventId))
                .ToListAsync(cancellationToken);

        var closureEvents = closureEventIds.Count == 0
            ? new List<BzaClosureEvent>()
            : await _context.ClosureEvents
                .AsNoTracking()
                .Where(c => closureEventIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

        var groupDeliveries = closureEventIds.Count == 0
            ? new List<BzaClosureGroupDelivery>()
            : await _context.ClosureGroupDeliveries
                .AsNoTracking()
                .Where(g => closureEventIds.Contains(g.BzaClosureEventId))
                .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Reporte de eventos");
        var headers = new[]
        {
            "Evento", "Cliente", "Total", "Productos", "Estatus de pago",
            "Método de pago", "Fecha de entrega", "Fecha de pago"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var row = 2;
        foreach (var sale in sales)
        {
            var statusName = "Pendiente de pago";
            string? paymentMethodName = null;
            DateTime? deliveryDate = null;
            DateTime? paymentDate = null;

            if (sale.BzaClosureEventId.HasValue)
            {
                var total = closureTotals.FirstOrDefault(t =>
                    t.BzaClosureEventId == sale.BzaClosureEventId.Value &&
                    t.BzaCustomerId == sale.BzaCustomerId);

                if (total is not null)
                {
                    statusName = total.Status switch
                    {
                        BzaClosureCustomerTotalStatus.Validated => "Pagado",
                        BzaClosureCustomerTotalStatus.Cancelled => "Cancelado",
                        _ => "Pendiente de pago"
                    };

                    if (total.Status == BzaClosureCustomerTotalStatus.Validated)
                    {
                        paymentMethodName = PaymentMethodNames.GetValueOrDefault(total.PaymentMethod, "No especificado");
                        paymentDate = total.ProofUploadedAt;
                    }
                }

                var collectorGroupId = sale.Customer.Collector?.BzaCollectorGroupId;
                if (collectorGroupId.HasValue)
                {
                    deliveryDate = groupDeliveries
                        .FirstOrDefault(g =>
                            g.BzaClosureEventId == sale.BzaClosureEventId.Value &&
                            g.BzaCollectorGroupId == collectorGroupId.Value)?.DeliveryDate;
                }

                deliveryDate ??= closureEvents
                    .FirstOrDefault(c => c.Id == sale.BzaClosureEventId.Value)?.OfficialDeliveryDate;
            }

            var productsText = string.Join(", ", sale.Products.Select(p => p.Description));

            ws.Cell(row, 1).Value = sale.Event.Description;
            ws.Cell(row, 2).Value = sale.Customer.Name;
            ws.Cell(row, 3).Value = sale.Total;
            ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 4).Value = productsText;
            ws.Cell(row, 5).Value = statusName;
            ws.Cell(row, 6).Value = paymentMethodName ?? "";
            ws.Cell(row, 7).Value = deliveryDate.HasValue ? deliveryDate.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(row, 8).Value = paymentDate.HasValue ? paymentDate.Value.ToString("dd/MM/yyyy") : "";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        var fileName = $"ReporteEventos_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return new BzaEventsReportResult(
            ms.ToArray(),
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
