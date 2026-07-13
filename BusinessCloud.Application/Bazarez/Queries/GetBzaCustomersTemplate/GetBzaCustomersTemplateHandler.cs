using BusinessCloud.Application.Common.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaCustomersTemplate;

public class GetBzaCustomersTemplateHandler(IBazaresDbContext context)
    : IRequestHandler<GetBzaCustomersTemplateQuery, BzaCustomersTemplateResult>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<BzaCustomersTemplateResult> Handle(GetBzaCustomersTemplateQuery request, CancellationToken ct)
    {
        // La plantilla es para dar de alta clientes NUEVOS: no se precarga el listado
        // de clientes existentes. Solo se ofrece el catálogo de recolectores.
        var collectors = await _context.Collectors
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();

        // --- Hoja de captura (la importacion lee la hoja "Clientes") ---
        // Columnas: 1=Nombre, 2=Telefono, 3=Recolector, 4=Nombre de Facebook.
        // Solo el Nombre es obligatorio en el archivo; el resto se completa/valida al subir.
        var ws = workbook.Worksheets.Add("Clientes");
        var headers = new[] { "Nombre", "Teléfono", "Recolector", "Nombre de Facebook" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // --- Hoja oculta con el catalogo de recolectores para la lista desplegable ---
        var catWs = workbook.Worksheets.Add("_Catalogos");
        for (int i = 0; i < collectors.Count; i++)
            catWs.Cell(i + 1, 1).Value = collectors[i];

        const int lastDataRow = 1000;

        // Lista desplegable de RECOLECTORES en la columna C.
        if (collectors.Count > 0)
        {
            var collectorRange = catWs.Range(1, 1, collectors.Count, 1);
            var collectorValidation = ws.Range(2, 3, lastDataRow, 3).CreateDataValidation();
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

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        var fileName = $"PlantillaClientes_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return new BzaCustomersTemplateResult(ms.ToArray(), fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
