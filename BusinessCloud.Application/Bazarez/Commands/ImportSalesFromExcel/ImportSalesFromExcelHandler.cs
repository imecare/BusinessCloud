using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.ImportSalesFromExcel;

public class ImportSalesFromExcelHandler : IRequestHandler<ImportSalesFromExcelCommand, ImportSalesResult>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public ImportSalesFromExcelHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<ImportSalesResult> Handle(ImportSalesFromExcelCommand request, CancellationToken ct)
    {
        var result = new ImportSalesResult();

        using var stream = new MemoryStream(request.FileContent);
        using var workbook = new XLWorkbook(stream);

        var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Ventas Live");
        if (ws == null)
        {
            result.Errors.Add("No se encontró la hoja 'Ventas Live' en el archivo.");
            return result;
        }

        // Cargar catálogos existentes para matching
        var existingCustomers = await _context.Customers
            .Include(c => c.Collector)
            .ToListAsync(ct);

        var collectors = await _context.Collectors.ToListAsync(ct);
        var collectorsByName = collectors.ToDictionary(c => c.Name.Trim().ToLowerInvariant(), c => c);

        // Agrupar filas por cliente (un cliente puede tener múltiples productos)
        var salesByCustomer = new Dictionary<string, List<(string Product, decimal Price, string? Notes)>>();
        var customerData = new Dictionary<string, (string Phone, string? Facebook, string Collector)>();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            result.TotalRows++;

            var clientName = ws.Cell(row, 1).GetString().Trim();
            var phone = ws.Cell(row, 2).GetString().Trim();
            var facebook = ws.Cell(row, 3).GetString().Trim();
            var collectorName = ws.Cell(row, 4).GetString().Trim();
            var product = ws.Cell(row, 5).GetString().Trim();
            var priceStr = ws.Cell(row, 6).GetString().Trim();
            var notes = ws.Cell(row, 7).GetString().Trim();

            if (string.IsNullOrEmpty(clientName) || string.IsNullOrEmpty(product))
            {
                if (!string.IsNullOrEmpty(clientName) || !string.IsNullOrEmpty(product))
                    result.Warnings.Add($"Fila {row}: datos incompletos, se omitió.");
                continue;
            }

            if (!decimal.TryParse(priceStr, out var price) || price <= 0)
            {
                result.Errors.Add($"Fila {row}: precio inválido '{priceStr}' para '{clientName} - {product}'.");
                continue;
            }

            if (string.IsNullOrEmpty(collectorName))
            {
                result.Errors.Add($"Fila {row}: recolector vacío para '{clientName}'.");
                continue;
            }

            if (!collectorsByName.ContainsKey(collectorName.ToLowerInvariant()))
            {
                result.Errors.Add($"Fila {row}: recolector '{collectorName}' no existe en el catálogo.");
                continue;
            }

            var key = $"{clientName}|{phone}".ToLowerInvariant();
            if (!salesByCustomer.ContainsKey(key))
            {
                salesByCustomer[key] = new List<(string, decimal, string?)>();
                customerData[key] = (phone, string.IsNullOrEmpty(facebook) ? null : facebook, collectorName);
            }
            salesByCustomer[key].Add((product, price, string.IsNullOrEmpty(notes) ? null : notes));
        }

        if (result.Errors.Count > 0)
            return result; // No procesar si hay errores de validación

        // Procesar ventas por cliente
        foreach (var (key, products) in salesByCustomer)
        {
            var (phone, facebook, collectorName) = customerData[key];
            var clientName = key.Split('|')[0];

            // Match de cliente: por teléfono o nombre de Facebook
            var customer = existingCustomers.FirstOrDefault(c =>
                (!string.IsNullOrEmpty(phone) && c.Phone.Trim().ToLowerInvariant() == phone.ToLowerInvariant()) ||
                (!string.IsNullOrEmpty(facebook) && c.FacebookName != null && c.FacebookName.Trim().ToLowerInvariant() == facebook.ToLowerInvariant()));

            if (customer == null)
            {
                // Auto-crear cliente nuevo
                var collector = collectorsByName[collectorName.ToLowerInvariant()];
                customer = new BzaCustomer
                {
                    Name = clientName,
                    Phone = phone,
                    FacebookName = facebook,
                    BzaCollectorId = collector.Id,
                    Status = 1,
                    PortalToken = Guid.NewGuid().ToString("N")[..12]
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync(ct);
                existingCustomers.Add(customer);
                result.CustomersCreated++;
            }

            var sale = new BzaSale
            {
                BzaCustomerId = customer.Id,
                Description = $"Importación Excel - {products.Count} artículos",
                Status = 1,
                Total = products.Sum(p => p.Price),
                PaymentDeadline = DateTime.UtcNow.AddDays(7),
                LabelCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                Products = products.Select(p => new BzaProduct
                {
                    Description = p.Product,
                    Price = p.Price
                }).ToList()
            };

            _context.Sales.Add(sale);
            result.SalesCreated++;
        }

        await _context.SaveChangesAsync(ct);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_BulkImport",
            TotalRows = result.TotalRows,
            SalesCreated = result.SalesCreated,
            CustomersCreated = result.CustomersCreated,
            Timestamp = DateTime.UtcNow
        }, ct);

        return result;
    }
}
