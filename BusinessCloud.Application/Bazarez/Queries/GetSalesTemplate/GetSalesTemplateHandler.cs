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

        // --- Hoja 1: Captura de compras (la importacion lee la hoja "Compras") ---
        // Columnas alineadas con el parser de importacion:
        // 1=Cliente, 2=Producto, 3=Precio, 4=Recolector, 5=Nombre de Facebook, 6=Telefono
        // Recolector, Facebook y Telefono son OPCIONALES:
        //  - Si el cliente ya existe y la celda viene en blanco, se conserva el dato registrado.
        //  - Facebook y Telefono solo se capturan (crean) para clientes NUEVOS.
        //  - Si el cliente existe y el dato difiere, se solicita confirmacion al validar.
        var ws = workbook.Worksheets.Add("Compras");
        var headers = new[] { "Cliente (Nombre)", "Producto", "Precio", "Recolector", "Nombre de Facebook", "Teléfono" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // --- Hoja oculta con catalogos para las listas desplegables ---
        // Columna 1 = clientes, Columna 2 = recolectores
        var catWs = workbook.Worksheets.Add("_Catalogos");
        for (int i = 0; i < customers.Count; i++)
            catWs.Cell(i + 1, 1).Value = customers[i].Name;
        for (int i = 0; i < collectors.Count; i++)
            catWs.Cell(i + 1, 2).Value = collectors[i];

        const int lastDataRow = 1000;
        const int collectorColumn = 4; // Recolector

        // Lista desplegable de CLIENTES en la columna A.
        // ErrorStyle = Warning permite escribir clientes nuevos (no bloquea la captura).
        if (customers.Count > 0)
        {
            var customerRange = catWs.Range(1, 1, customers.Count, 1);
            var customerValidation = ws.Range(2, 1, lastDataRow, 1).CreateDataValidation();
            customerValidation.List(customerRange);
            customerValidation.IgnoreBlanks = true;
            customerValidation.InCellDropdown = true;
            customerValidation.ErrorStyle = XLErrorStyle.Warning;
            customerValidation.ShowErrorMessage = true;
            customerValidation.ErrorTitle = "Cliente nuevo";
            customerValidation.ErrorMessage =
                "Este cliente no esta en la lista. Si es nuevo, elige \"Si\" para agregarlo; se completaran sus datos al subir el archivo.";
        }

        // Lista desplegable de RECOLECTORES en la columna D (opcional).
        if (collectors.Count > 0)
        {
            var collectorRange = catWs.Range(1, 2, collectors.Count, 2);
            var collectorValidation = ws.Range(2, collectorColumn, lastDataRow, collectorColumn).CreateDataValidation();
            collectorValidation.List(collectorRange);
            collectorValidation.IgnoreBlanks = true;
            collectorValidation.InCellDropdown = true;
            collectorValidation.ErrorStyle = XLErrorStyle.Warning;
            collectorValidation.ShowErrorMessage = true;
            collectorValidation.ErrorTitle = "Recolector nuevo";
            collectorValidation.ErrorMessage =
                "Este recolector no esta en la lista. Verifica el nombre o eligelo del listado.";
        }

        catWs.Hide();
        ws.Columns().AdjustToContents();

        // --- Hoja 2: Catalogo de clientes registrados (solo referencia) ---
        var refWs = workbook.Worksheets.Add("Clientes Registrados");
        var refHeaders = new[] { "Nombre", "Recolector" };
        for (int i = 0; i < refHeaders.Length; i++)
        {
            refWs.Cell(1, i + 1).Value = refHeaders[i];
            refWs.Cell(1, i + 1).Style.Font.Bold = true;
        }
        for (int i = 0; i < customers.Count; i++)
        {
            refWs.Cell(i + 2, 1).Value = customers[i].Name;
            refWs.Cell(i + 2, 2).Value = customers[i].CollectorName;
        }
        refWs.Columns().AdjustToContents();
        refWs.Protect();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        var fileName = $"PlantillaVentas_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return new SalesTemplateResult(ms.ToArray(), fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
