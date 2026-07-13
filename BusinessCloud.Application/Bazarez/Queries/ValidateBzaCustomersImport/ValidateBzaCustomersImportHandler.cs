using BusinessCloud.Application.Common.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.ValidateBzaCustomersImport;

public class ValidateBzaCustomersImportHandler(IBazaresDbContext context)
    : IRequestHandler<ValidateBzaCustomersImportQuery, ValidateBzaCustomersImportResult>
{
    private readonly IBazaresDbContext _context = context;

    public async Task<ValidateBzaCustomersImportResult> Handle(ValidateBzaCustomersImportQuery request, CancellationToken ct)
    {
        var result = new ValidateBzaCustomersImportResult();

        // 1. Abrir el archivo
        using var stream = new MemoryStream(request.FileContent);
        using var workbook = new XLWorkbook(stream);

        var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Clientes")
              ?? workbook.Worksheets.FirstOrDefault(w => !w.Name.StartsWith("_"))
              ?? workbook.Worksheets.FirstOrDefault();

        if (ws is null)
        {
            result.Errors.Add("No se encontró ninguna hoja en el archivo Excel.");
            return result;
        }

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // 2. Parsear filas: [Nombre, Teléfono, Recolector, Facebook]
        // El nombre puede venir escrito directamente o elegido de una lista desplegable;
        // en ambos casos se lee el valor de la celda. Se agrupa por nombre (normalizado).
        var rows = new Dictionary<string, ImportCustomerRowDto>(StringComparer.OrdinalIgnoreCase);

        for (int row = 2; row <= lastRow; row++)
        {
            var name = ws.Cell(row, 1).GetString().Trim();
            var phone = NormalizePhone(ws.Cell(row, 2).GetString());
            var collectorName = ws.Cell(row, 3).GetString().Trim();
            var facebookName = ws.Cell(row, 4).GetString().Trim();

            if (string.IsNullOrEmpty(name))
                continue;

            if (!rows.TryGetValue(name, out var existing))
            {
                rows[name] = new ImportCustomerRowDto
                {
                    Name = name,
                    PhoneFromFile = phone,
                    CollectorNameFromFile = collectorName,
                    FacebookNameFromFile = facebookName,
                };
                result.TotalRows++;
            }
            else
            {
                // Fila repetida para el mismo nombre: conservar el primer valor no vacío.
                if (string.IsNullOrEmpty(existing.PhoneFromFile) && phone.Length > 0)
                    existing.PhoneFromFile = phone;
                if (string.IsNullOrEmpty(existing.CollectorNameFromFile) && collectorName.Length > 0)
                    existing.CollectorNameFromFile = collectorName;
                if (string.IsNullOrEmpty(existing.FacebookNameFromFile) && facebookName.Length > 0)
                    existing.FacebookNameFromFile = facebookName;

                result.Errors.Add($"Nombre repetido en el archivo: '{name}'. Se tomará una sola vez.");
            }
        }

        result.HasRows = result.TotalRows > 0;

        // 3. Catálogos
        var collectors = await _context.Collectors
            .OrderBy(c => c.Name)
            .Select(c => new ImportCollectorDto { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);
        result.Collectors = collectors;

        result.CollectorGroups = await _context.CollectorGroups
            .OrderBy(g => g.Description)
            .Select(g => new ImportCollectorGroupDto { Id = g.Id, Description = g.Description })
            .ToListAsync(ct);

        var existingCustomers = await _context.Customers
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToListAsync(ct);

        // Índice de teléfonos ya usados -> cliente dueño
        var phoneOwners = existingCustomers
            .Where(c => !string.IsNullOrWhiteSpace(c.Phone))
            .GroupBy(c => c.Phone.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var existingCollectorNames = new HashSet<string>(
            collectors.Select(c => c.Name.Trim()), StringComparer.OrdinalIgnoreCase);

        // 4. Resolver el estado de cada cliente
        foreach (var dto in rows.Values)
        {
            var fileCol = dto.CollectorNameFromFile.Trim();
            if (fileCol.Length > 0)
            {
                var col = collectors.FirstOrDefault(c =>
                    c.Name.Trim().Equals(fileCol, StringComparison.OrdinalIgnoreCase));
                dto.SuggestedCollectorId = col?.Id;
                dto.CollectorExists = col != null;
            }

            var match = existingCustomers.FirstOrDefault(c =>
                c.Name.Trim().Equals(dto.Name, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                dto.MatchStatus = "existing";
                dto.MatchedCustomerId = match.Id;
            }
            else
            {
                dto.MatchStatus = "new";
            }

            // Conflicto de teléfono: pertenece a OTRO cliente distinto al coincidente.
            if (dto.PhoneFromFile.Length > 0
                && phoneOwners.TryGetValue(dto.PhoneFromFile, out var owner)
                && owner.Id != dto.MatchedCustomerId)
            {
                dto.PhoneConflict = true;
                dto.PhoneConflictCustomerName = owner.Name;
            }
        }

        // Recolectores del archivo que NO existen en BD (requieren alta + grupo)
        result.NewCollectors = rows.Values
            .Select(r => r.CollectorNameFromFile.Trim())
            .Where(n => n.Length > 0 && !existingCollectorNames.Contains(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        result.Customers = rows.Values
            .OrderBy(r => r.Name)
            .ToList();

        return result;
    }

    /// <summary>Deja solo los dígitos del teléfono para usarlo como llave única.</summary>
    private static string NormalizePhone(string? phone)
        => new((phone ?? string.Empty).Where(char.IsDigit).ToArray());
}
