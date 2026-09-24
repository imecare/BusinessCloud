using BusinessCloud.Application.Common.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetWhatsAppCustomersExcel;

/// <summary>Genera el Excel del reporte de clientes que tienen WhatsApp.</summary>
public record GetWhatsAppCustomersExcelQuery : IRequest<WhatsAppCustomersExcelResult>;

public record WhatsAppCustomersExcelResult(byte[] FileContent, string FileName, string ContentType);

public class GetWhatsAppCustomersExcelHandler(IBazaresDbContext context)
    : IRequestHandler<GetWhatsAppCustomersExcelQuery, WhatsAppCustomersExcelResult>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<WhatsAppCustomersExcelResult> Handle(
        GetWhatsAppCustomersExcelQuery request,
        CancellationToken cancellationToken)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(customer => !customer.HasNoWhatsApp && !string.IsNullOrEmpty(customer.Phone))
            .Include(customer => customer.Collector)
            .OrderBy(customer => customer.Name)
            .Select(customer => new
            {
                customer.Id,
                customer.Name,
                CollectorName = customer.Collector != null ? customer.Collector.Name : string.Empty,
                customer.Phone,
                customer.FacebookName,
            })
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clientes con WhatsApp");
        var headers = new[]
        {
            "Nombre del cliente", "Número de cliente", "Recolector", "Número de WhatsApp", "Perfil de Facebook",
        };

        for (var index = 0; index < headers.Length; index++)
        {
            var cell = worksheet.Cell(1, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        var row = 2;
        foreach (var customer in customers)
        {
            worksheet.Cell(row, 1).Value = customer.Name;
            worksheet.Cell(row, 2).Value = customer.Id;
            worksheet.Cell(row, 3).Value = customer.CollectorName;
            worksheet.Cell(row, 4).Value = customer.Phone;
            worksheet.Cell(row, 5).Value = customer.FacebookName ?? string.Empty;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new WhatsAppCustomersExcelResult(
            stream.ToArray(),
            $"ClientesConWhatsApp_{DateTime.UtcNow:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}