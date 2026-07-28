using BusinessCloud.Application.Common.Interfaces;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.ValidateBzaImport;

public class ValidateBzaImportHandler(IBazaresDbContext context)
    : IRequestHandler<ValidateBzaImportQuery, ValidateBzaImportResult>
{
    private readonly IBazaresDbContext _context = context;

    private const int DuplicateSampleSize = 10;

    public async Task<ValidateBzaImportResult> Handle(ValidateBzaImportQuery request, CancellationToken ct)
    {
        var result = new ValidateBzaImportResult();

        // 1. Validar que el Evento exista
        var saleEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == request.EventId, ct)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        if (saleEvent.Status == 5)
            throw new InvalidOperationException("No se puede importar en un evento cancelado.");

        // Si el evento ya está en proceso de pago (tiene ventas enviadas a cobro),
        // no se pueden agregar más ventas mediante el importador.
        var eventInPayment = await _context.Sales
            .AnyAsync(s => s.BzaEventId == request.EventId && s.BzaClosureEventId != null, ct);
        if (eventInPayment)
            throw new InvalidOperationException(
                "El evento ya está en proceso de pago (se enviaron totales a los clientes). No se pueden agregar más ventas a este evento.");

        // 2. Abrir el archivo
        using var stream = new MemoryStream(request.FileContent);
        using var workbook = new XLWorkbook(stream);

        var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Compras")
              ?? workbook.Worksheets.FirstOrDefault();

        if (ws is null)
        {
            result.Errors.Add("No se encontró ninguna hoja en el archivo Excel.");
            return result;
        }

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // 3. Parsear filas: [Producto, Precio, Cliente, Recolector, Facebook, Telefono]
        // Agrupar por nombre de cliente (normalizado).
        var groups = new Dictionary<string, ImportCustomerGroupDto>(StringComparer.OrdinalIgnoreCase);
        var sampleItems = new List<(string Description, decimal Price)>();

        for (int row = 2; row <= lastRow; row++)
        {
            var productDesc = ws.Cell(row, 1).GetString().Trim();
            var priceStr = ws.Cell(row, 2).GetString().Trim();
            var clientName = ws.Cell(row, 3).GetString().Trim();
            var collectorName = ws.Cell(row, 4).GetString().Trim();
            var facebookName = ws.Cell(row, 5).GetString().Trim();
            var phone = ws.Cell(row, 6).GetString().Trim();

            if (string.IsNullOrEmpty(clientName) && string.IsNullOrEmpty(productDesc))
                continue;

            if (string.IsNullOrEmpty(clientName))
            {
                result.Errors.Add($"Fila {row}: nombre de cliente vacío.");
                continue;
            }

            if (string.IsNullOrEmpty(productDesc))
            {
                result.Errors.Add($"Fila {row}: producto vacío para '{clientName}'.");
                continue;
            }

            // Precio: si es inválido NO se descarta el producto; se marca para que
            // el usuario lo capture en la pantalla de validación antes de confirmar.
            var hasValidPrice = decimal.TryParse(priceStr, out var price) && price > 0;
            if (!hasValidPrice)
                price = 0m;

            if (!groups.TryGetValue(clientName, out var group))
            {
                group = new ImportCustomerGroupDto
                {
                    CustomerName = clientName,
                    CollectorNameFromFile = collectorName,
                    FacebookNameFromFile = facebookName,
                    PhoneFromFile = phone,
                };
                groups[clientName] = group;
            }
            else
            {
                // Conservar el primer valor no vacío detectado para cada dato del cliente.
                if (string.IsNullOrEmpty(group.CollectorNameFromFile) && !string.IsNullOrEmpty(collectorName))
                    group.CollectorNameFromFile = collectorName;
                if (string.IsNullOrEmpty(group.FacebookNameFromFile) && !string.IsNullOrEmpty(facebookName))
                    group.FacebookNameFromFile = facebookName;
                if (string.IsNullOrEmpty(group.PhoneFromFile) && !string.IsNullOrEmpty(phone))
                    group.PhoneFromFile = phone;
            }

            group.Products.Add(new ImportProductLineDto
            {
                Description = productDesc,
                Price = price,
                PriceMissing = !hasValidPrice,
                RawPrice = hasValidPrice ? null : priceStr,
            });
            group.Total += price;
            result.TotalProducts++;

            if (sampleItems.Count < DuplicateSampleSize)
                sampleItems.Add((productDesc, price));
        }

        result.HasRows = result.TotalProducts > 0;

        // 4. Catálogos
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
            .Include(c => c.Collector)
            .Select(c => new { c.Id, c.Name, c.Phone, c.FacebookName, CollectorId = c.BzaCollectorId, CollectorName = c.Collector.Name })
            .ToListAsync(ct);

        var existingCollectorNames = new HashSet<string>(
            collectors.Select(c => c.Name.Trim()), StringComparer.OrdinalIgnoreCase);

        // 5. Resolver el estado de cada cliente
        foreach (var group in groups.Values)
        {
            var fileCol = group.CollectorNameFromFile.Trim();

            // Recolector sugerido / existencia (por nombre del archivo)
            if (fileCol.Length > 0)
            {
                var col = collectors.FirstOrDefault(c =>
                    c.Name.Trim().Equals(fileCol, StringComparison.OrdinalIgnoreCase));
                group.SuggestedCollectorId = col?.Id;
                group.CollectorExists = col != null;
            }

            var matches = existingCustomers
                .Where(c => c.Name.Trim().Equals(group.CustomerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                group.MatchStatus = "new";
            }
            else if (matches.Count == 1)
            {
                group.MatchStatus = "existing";
                group.MatchedCustomerId = matches[0].Id;
                group.CurrentCollectorId = matches[0].CollectorId;
                group.CurrentCollectorName = matches[0].CollectorName;
                group.CurrentFacebookName = matches[0].FacebookName;
                group.CurrentPhone = matches[0].Phone;

                // El recolector del archivo difiere del registrado -> requiere confirmación de cambio
                if (fileCol.Length > 0 &&
                    !matches[0].CollectorName.Trim().Equals(fileCol, StringComparison.OrdinalIgnoreCase))
                {
                    group.CollectorChanged = true;
                }

                // El Facebook del archivo difiere del registrado -> requiere confirmación de cambio
                var fileFacebook = group.FacebookNameFromFile.Trim();
                if (fileFacebook.Length > 0 &&
                    !fileFacebook.Equals((matches[0].FacebookName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    group.FacebookChanged = true;
                }

                // El teléfono del archivo difiere del registrado -> requiere confirmación de cambio
                var filePhone = group.PhoneFromFile.Trim();
                if (filePhone.Length > 0 &&
                    !filePhone.Equals((matches[0].Phone ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    group.PhoneChanged = true;
                }
            }
            else
            {
                group.MatchStatus = "ambiguous";
                group.Candidates = matches.Select(m => new ImportCandidateDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Phone = m.Phone,
                    CollectorName = m.CollectorName,
                }).ToList();
            }
        }

        // Recolectores presentes en el archivo que NO existen en BD (requieren alta + grupo)
        result.NewCollectors = groups.Values
            .Select(g => g.CollectorNameFromFile.Trim())
            .Where(n => n.Length > 0 && !existingCollectorNames.Contains(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        result.Customers = groups.Values
            .OrderBy(g => g.CustomerName)
            .ToList();

        // 6. Deteccion de posible archivo ya subido: compara los primeros productos
        //    (descripcion + precio, para evitar falsos positivos por nombres genericos)
        //    contra productos de eventos ABIERTOS. Solo advierte si hay una coincidencia
        //    fuerte (al menos 2 productos y al menos la mitad de la muestra).
        if (sampleItems.Count > 0)
        {
            var sample = sampleItems
                .Where(x => x.Description.Length > 0)
                .Select(x => (Description: x.Description.Trim(), x.Price))
                .Distinct()
                .ToList();

            var sampleDescriptions = sample
                .Select(s => s.Description)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var candidateMatches = sampleDescriptions.Count > 0
                ? await _context.SoldProducts
                    .Where(p => p.Sale.Event.Status == 1
                                && sampleDescriptions.Contains(p.Description))
                    .Select(p => new { p.Description, p.Price, EventName = p.Sale.Event.Description })
                    .ToListAsync(ct)
                : [];

            var matchedPairs = candidateMatches
                .Where(cm => sample.Any(s =>
                    s.Description.Equals(cm.Description, StringComparison.OrdinalIgnoreCase) && s.Price == cm.Price))
                .ToList();

            var matchedDescriptions = matchedPairs
                .Select(m => m.Description)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var matchRatio = sample.Count > 0 ? (double)matchedDescriptions.Count / sample.Count : 0;

            if (matchedDescriptions.Count >= 2 && matchRatio >= 0.5)
            {
                var eventNames = matchedPairs
                    .Select(d => d.EventName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                result.DuplicateWarning = new ImportDuplicateWarningDto
                {
                    PossibleDuplicate = true,
                    MatchedDescriptions = matchedDescriptions,
                    EventNames = eventNames,
                    Message = $"Se encontraron {matchedDescriptions.Count} de los primeros {sample.Count} productos (mismo nombre y precio) ya registrados " +
                              $"en evento(s) abierto(s): {string.Join(", ", eventNames)}. " +
                              "Es posible que este archivo ya se haya subido antes. Revisa antes de continuar.",
                };
            }
        }

        // 7. Sugerencias de clientes duplicados/similares para evitar altas repetidas
        //    por errores de captura (ej. "Mariana Prueb" vs "Mariana prueba").
        var newGroups = groups.Values.Where(g => g.MatchStatus == "new").ToList();

        // 7.a. Nuevo vs. existentes en BD: si el nombre es muy parecido a un cliente ya
        //      registrado, se sugiere usar ese cliente en vez de crear uno nuevo.
        foreach (var group in newGroups)
        {
            var closeMatches = existingCustomers
                .Where(c => IsFuzzyMatch(group.CustomerName, c.Name))
                .Select(c => new ImportCandidateDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    CollectorName = c.CollectorName,
                })
                .ToList();

            if (closeMatches.Count > 0)
                group.PossibleExistingMatches = closeMatches;
        }

        // 7.b. Nuevo vs. nuevo dentro del mismo archivo: nombres muy parecidos que
        //      probablemente son el mismo cliente capturado con una variacion/error.
        for (int i = 0; i < newGroups.Count; i++)
        {
            for (int j = i + 1; j < newGroups.Count; j++)
            {
                if (IsFuzzyMatch(newGroups[i].CustomerName, newGroups[j].CustomerName))
                {
                    result.PossibleDuplicateNewCustomers.Add(new ImportDuplicatePairDto
                    {
                        NameA = newGroups[i].CustomerName,
                        NameB = newGroups[j].CustomerName,
                    });
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Compara dos nombres y determina si son lo suficientemente parecidos como para
    /// tratarse probablemente de la misma persona (typo/variacion de captura), usando
    /// distancia de Levenshtein normalizada por la longitud del nombre mas largo.
    /// </summary>
    private static bool IsFuzzyMatch(string nameA, string nameB)
    {
        var a = (nameA ?? string.Empty).Trim();
        var b = (nameB ?? string.Empty).Trim();

        if (a.Length == 0 || b.Length == 0)
            return false;

        if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            return false; // ya son iguales (mismo grupo), no aplica sugerencia

        var maxLen = Math.Max(a.Length, b.Length);
        if (maxLen < 5)
            return false; // nombres muy cortos: evitar falsos positivos

        var distance = LevenshteinDistance(a, b);
        if (distance == 0)
            return false;

        // Tolerancia: 1 caracter de diferencia por cada ~6 caracteres del nombre,
        // con un minimo de 1 y un maximo de 3 caracteres de diferencia permitidos.
        var allowedDistance = Math.Clamp(maxLen / 6, 1, 3);
        return distance <= allowedDistance;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        s = s.ToLowerInvariant();
        t = t.ToLowerInvariant();
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
