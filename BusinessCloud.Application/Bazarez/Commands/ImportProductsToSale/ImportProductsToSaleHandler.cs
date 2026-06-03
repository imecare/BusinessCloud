using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.ImportProductsToSale;

public class ImportProductsToSaleHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<ImportProductsToSaleCommand, ImportProductsResult>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<ImportProductsResult> Handle(ImportProductsToSaleCommand request, CancellationToken ct)
    {
        var result = new ImportProductsResult();

        // 1. Validar que el Evento de Venta exista
        var saleEvent = await _context.Sales.FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, ct)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        if (saleEvent.Status == 5)
            throw new InvalidOperationException("No se puede importar productos en un evento cancelado.");

        using var stream = new MemoryStream(request.FileContent);
        using var workbook = new XLWorkbook(stream);

        var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Compras")
              ?? workbook.Worksheets.FirstOrDefault();

        if (ws is null)
        {
            result.Errors.Add("No se encontró ninguna hoja en el archivo Excel.");
            return result;
        }

        // 2. Cargar catálogos existentes
        var existingCustomers = await _context.Customers.ToListAsync(ct);
        var collectors = await _context.Collectors.ToListAsync(ct);
        var defaultCollector = collectors.FirstOrDefault()
            ?? throw new InvalidOperationException("No hay recolectores configurados en el sistema.");

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        // 3. Procesar filas: [Cliente, Teléfono, Producto, Precio]
        for (int row = 2; row <= lastRow; row++)
        {
            var clientName = ws.Cell(row, 1).GetString().Trim();
            var phone = ws.Cell(row, 2).GetString().Trim();
            var productDesc = ws.Cell(row, 3).GetString().Trim();
            var priceStr = ws.Cell(row, 4).GetString().Trim();

            // Validar fila vacía
            if (string.IsNullOrEmpty(clientName) && string.IsNullOrEmpty(productDesc))
                continue;

            // Validaciones
            if (string.IsNullOrEmpty(clientName))
            {
                result.Errors.Add($"Fila {row}: nombre de cliente vacío.");
                result.Failed++;
                continue;
            }

            if (string.IsNullOrEmpty(productDesc))
            {
                result.Errors.Add($"Fila {row}: descripción de producto vacía para '{clientName}'.");
                result.Failed++;
                continue;
            }

            if (!decimal.TryParse(priceStr, out var price) || price <= 0)
            {
                result.Errors.Add($"Fila {row}: precio inválido '{priceStr}' para '{clientName} - {productDesc}'.");
                result.Failed++;
                continue;
            }

            // 4. Buscar o crear cliente
            var customer = existingCustomers.FirstOrDefault(c =>
                (!string.IsNullOrEmpty(phone) && c.Phone.Trim().Equals(phone, StringComparison.OrdinalIgnoreCase)) ||
                c.Name.Trim().Equals(clientName, StringComparison.OrdinalIgnoreCase));

            if (customer is null)
            {
                customer = new BzaCustomer
                {
                    Name = clientName,
                    Phone = phone,
                    BzaCollectorId = defaultCollector.Id,
                    Status = 1,
                    PortalToken = Guid.NewGuid().ToString("N")[..12]
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync(ct);
                existingCustomers.Add(customer);
                result.NewCustomersCreated++;
            }

            // 5. Crear producto vendido
            var product = new BzaSoldProduct
            {
                BzaSaleId = request.BzaSaleId,
                BzaCustomerId = customer.Id,
                Description = productDesc,
                Price = price
            };

            _context.SoldProducts.Add(product);
            result.ImportedProducts++;
        }

        await _context.SaveChangesAsync(ct);

        // 6. Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_ProductsImported",
            SaleEventId = saleEvent.Id,
            SaleEventDescription = saleEvent.Description,
            ImportedProducts = result.ImportedProducts,
            NewCustomersCreated = result.NewCustomersCreated,
            Failed = result.Failed,
            Timestamp = DateTime.UtcNow
        }, ct);

        return result;
    }
}
