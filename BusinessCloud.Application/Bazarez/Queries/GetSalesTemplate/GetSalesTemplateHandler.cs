using BusinessCloud.Application.Common.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetSalesTemplate;

public class GetSalesTemplateHandler : IRequestHandler<GetSalesTemplateQuery, SalesTemplateResult>
{
    private readonly IBazaresDbContext _context;

    public GetSalesTemplateHandler(IBazaresDbContext context) => _context = context;

    public async Task<SalesTemplateResult> Handle(GetSalesTemplateQuery request, CancellationToken ct)
    {
        var customers = await _context.Customers
            .Include(c => c.Collector)
            .Where(c => c.Status == 1)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Name, c.Phone, c.FacebookName, CollectorName = c.Collector.Name })
            .ToListAsync(ct);

        var collectors = await _context.Collectors
            .Select(c => c.Name)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();

        // --- Hoja 1: Plantilla de Ventas ---
        var ws = workbook.Worksheets.Add("Ventas Live");
        var headers = new[] { "Cliente (Nombre)", "Teléfono", "Facebook", "Recolector", "Producto", "Precio", "Notas" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Pre-llenar con clientes existentes (una fila por cliente para facilitar captura)
        for (int i = 0; i < customers.Count; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = customers[i].Name;
            ws.Cell(row, 2).Value = customers[i].Phone;
            ws.Cell(row, 3).Value = customers[i].FacebookName ?? "";
            ws.Cell(row, 4).Value = customers[i].CollectorName;
        }

        // Validación de datos: dropdown de recolectores en columna D
        if (collectors.Count > 0)
        {
            var catWs = workbook.Worksheets.Add("_Catalogos");
            for (int i = 0; i < collectors.Count; i++)
                catWs.Cell(i + 1, 1).Value = collectors[i];

            var range = catWs.Range(1, 1, collectors.Count, 1);
            var validation = ws.Range(2, 4, 500, 4).CreateDataValidation();
            validation.List(range);
            catWs.Hide();
        }

        ws.Columns().AdjustToContents();

        // --- Hoja 2: Catálogo de clientes (referencia) ---
        var refWs = workbook.Worksheets.Add("Clientes Registrados");
        refWs.Cell(1, 1).Value = "Nombre";
        refWs.Cell(1, 2).Value = "Teléfono";
        refWs.Cell(1, 3).Value = "Facebook";
        refWs.Cell(1, 4).Value = "Recolector";
        for (int i = 0; i < customers.Count; i++)
        {
            refWs.Cell(i + 2, 1).Value = customers[i].Name;
            refWs.Cell(i + 2, 2).Value = customers[i].Phone;
            refWs.Cell(i + 2, 3).Value = customers[i].FacebookName ?? "";
            refWs.Cell(i + 2, 4).Value = customers[i].CollectorName;
        }
        refWs.Columns().AdjustToContents();
        refWs.Protect();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        var fileName = $"PlantillaVentas_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return new SalesTemplateResult(ms.ToArray(), fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
