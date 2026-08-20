using System.Globalization;
using System.Text;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.ValidateBzaCustomersImport;

public class ValidateBzaCustomersImportHandler(IBazaresDbContext context)
    : IRequestHandler<ValidateBzaCustomersImportQuery, ValidateBzaCustomersImportResult>
{
    public async Task<ValidateBzaCustomersImportResult> Handle(
        ValidateBzaCustomersImportQuery request,
        CancellationToken ct)
    {
        var result = new ValidateBzaCustomersImportResult();
        using var stream = new MemoryStream(request.FileContent);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == "Clientes")
            ?? workbook.Worksheets.FirstOrDefault(sheet => !sheet.Name.StartsWith("_", StringComparison.Ordinal))
            ?? workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            result.Errors.Add("No se encontró ninguna hoja en el archivo Excel.");
            return result;
        }

        var columns = ResolveColumns(worksheet);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var parsedRows = new List<ParsedCustomerRow>();

        for (var row = 2; row <= lastRow; row++)
        {
            var name = CollectorCatalogNameNormalizer.CollapseSpaces(
                worksheet.Cell(row, columns.Name).GetString());
            if (name.Length == 0)
                continue;

            var collectorName = columns.Collector is int collectorColumn
                ? CollectorCatalogNameNormalizer.Clean(worksheet.Cell(row, collectorColumn).GetString())
                : string.Empty;
            var phone = columns.Phone is int phoneColumn
                ? PhoneNumberNormalizer.Normalize(worksheet.Cell(row, phoneColumn).GetString())
                : string.Empty;
            var facebook = columns.Facebook is int facebookColumn
                ? CollectorCatalogNameNormalizer.CollapseSpaces(worksheet.Cell(row, facebookColumn).GetString())
                : string.Empty;

            parsedRows.Add(new ParsedCustomerRow(
                row,
                name,
                name.ToUpperInvariant(),
                phone,
                collectorName,
                CollectorCatalogNameNormalizer.ToComparisonKey(collectorName),
                facebook));
            result.TotalRows++;
        }

        result.HasRows = result.TotalRows > 0;
        var rows = new Dictionary<string, ImportCustomerRowDto>(StringComparer.Ordinal);

        foreach (var group in parsedRows.GroupBy(row => row.NameKey, StringComparer.Ordinal))
        {
            var entries = group.ToList();
            var first = entries[0];
            result.ExactDuplicateRows += entries.Count - 1;

            var collectorNames = entries
                .Where(row => row.CollectorKey.Length > 0)
                .GroupBy(row => row.CollectorKey, StringComparer.Ordinal)
                .Select(match => match.First().CollectorName)
                .ToList();

            var dto = new ImportCustomerRowDto
            {
                Name = first.Name,
                PhoneFromFile = entries.Select(row => row.Phone).FirstOrDefault(value => value.Length > 0) ?? string.Empty,
                FacebookNameFromFile = entries.Select(row => row.Facebook).FirstOrDefault(value => value.Length > 0) ?? string.Empty,
                CollectorNameFromFile = collectorNames.Count == 1 ? collectorNames[0] : string.Empty,
            };

            if (collectorNames.Count > 1)
            {
                dto.HasCollectorConflict = true;
                dto.CollectorConflictNames = collectorNames.OrderBy(name => name).ToList();
                result.CollectorConflictCount++;
                result.Errors.Add(
                    $"Cliente '{first.Name}' omitido de la resolución automática: aparece con recolectores distintos " +
                    $"({string.Join(" / ", dto.CollectorConflictNames)}). Selecciona el correcto antes de importar.");
            }

            rows.Add(group.Key, dto);
        }

        if (result.ExactDuplicateRows > 0)
        {
            result.Errors.Insert(0,
                $"Se deduplicaron {result.ExactDuplicateRows} fila(s) por coincidencia exacta de nombre, " +
                "ignorando mayúsculas/minúsculas y espacios repetidos.");
        }

        var collectors = await context.Collectors
            .AsNoTracking()
            .OrderBy(collector => collector.Name)
            .Select(collector => new ImportCollectorDto { Id = collector.Id, Name = collector.Name })
            .ToListAsync(ct);
        result.Collectors = collectors;

        result.CollectorGroups = await context.CollectorGroups
            .AsNoTracking()
            .OrderBy(group => group.Description)
            .Select(group => new ImportCollectorGroupDto { Id = group.Id, Description = group.Description })
            .ToListAsync(ct);

        var collectorLookup = collectors
            .GroupBy(collector => CollectorCatalogNameNormalizer.ToComparisonKey(collector.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var existingCustomers = await context.Customers
            .AsNoTracking()
            .Select(customer => new { customer.Id, customer.Name, customer.Phone })
            .ToListAsync(ct);
        var existingByName = existingCustomers
            .GroupBy(customer => CollectorCatalogNameNormalizer.CollapseSpaces(customer.Name).ToUpperInvariant(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var phoneOwners = existingCustomers
            .Where(customer => !string.IsNullOrWhiteSpace(customer.Phone))
            .GroupBy(customer => customer.Phone.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var entry in rows)
        {
            var dto = entry.Value;
            var collectorKey = CollectorCatalogNameNormalizer.ToComparisonKey(dto.CollectorNameFromFile);

            if (collectorKey.Length > 0 && collectorKey != "SIN ASIGNAR" && collectorLookup.TryGetValue(collectorKey, out var matches))
            {
                if (matches.Count == 1)
                {
                    dto.SuggestedCollectorId = matches[0].Id;
                    dto.CollectorExists = true;
                    dto.CollectorNameFromFile = matches[0].Name;
                }
                else
                {
                    dto.CollectorAmbiguous = true;
                    result.Errors.Add(
                        $"Cliente '{dto.Name}': el recolector '{dto.CollectorNameFromFile}' existe en más de un grupo. " +
                        "La fila no puede importarse hasta corregir esa ambigüedad.");
                }
            }
            if (existingByName.TryGetValue(entry.Key, out var match))
            {
                dto.MatchStatus = "existing";
                dto.MatchedCustomerId = match.Id;
            }

            dto.WillHaveNoWhatsApp = dto.PhoneFromFile.Length == 0;
            dto.WillBePendingInfo = dto.WillHaveNoWhatsApp
                && string.IsNullOrWhiteSpace(dto.FacebookNameFromFile);

            if (dto.PhoneFromFile.Length > 0
                && phoneOwners.TryGetValue(dto.PhoneFromFile, out var owner)
                && owner.Id != dto.MatchedCustomerId)
            {
                dto.PhoneConflict = true;
                dto.PhoneConflictCustomerName = owner.Name;
            }
        }

        result.NewCollectors = rows.Values
            .Select(row => row.CollectorNameFromFile)
            .Where(name =>
            {
                var key = CollectorCatalogNameNormalizer.ToComparisonKey(name);
                return key.Length > 0 && key != "SIN ASIGNAR" && !collectorLookup.ContainsKey(key);
            })
            .GroupBy(CollectorCatalogNameNormalizer.ToComparisonKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(name => name)
            .ToList();

        result.Customers = rows.Values.OrderBy(row => row.Name).ToList();
        return result;
    }

    private static ImportColumns ResolveColumns(IXLWorksheet worksheet)
    {
        var lastColumn = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
        var headers = Enumerable.Range(1, lastColumn)
            .ToDictionary(column => column, column => NormalizeHeader(worksheet.Cell(1, column).GetString()));

        int? Find(params string[] aliases)
            => headers.FirstOrDefault(pair => aliases.Contains(pair.Value, StringComparer.Ordinal)).Key is var column
                && column > 0 ? column : null;

        var name = Find("NOMBRE", "CLIENTE", "CLIENTA") ?? 1;
        var collector = Find("RECOLECTOR", "RECOLECTORA");
        var phone = Find("TELEFONO", "TELEFONO DE CONTACTO", "WHATSAPP", "CELULAR");
        var facebook = Find("FACEBOOK", "NOMBRE DE FACEBOOK", "USUARIO DE FACEBOOK");

        if (collector is null && lastColumn >= 3)
            collector = 3;
        if (phone is null && lastColumn >= 2 && collector != 2)
            phone = 2;
        if (facebook is null && lastColumn >= 4)
            facebook = 4;

        return new ImportColumns(name, phone, collector, facebook);
    }

    private static string NormalizeHeader(string? value)
    {
        var normalized = CollectorCatalogNameNormalizer.CollapseSpaces(value)
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record ParsedCustomerRow(
        int SourceRow,
        string Name,
        string NameKey,
        string Phone,
        string CollectorName,
        string CollectorKey,
        string Facebook);

    private sealed record ImportColumns(int Name, int? Phone, int? Collector, int? Facebook);
}





