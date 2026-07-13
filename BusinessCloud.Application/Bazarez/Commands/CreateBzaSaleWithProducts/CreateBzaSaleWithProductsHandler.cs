using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSaleWithProducts;

public class CreateBzaSaleWithProductsHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<CreateBzaSaleWithProductsCommand, CreateBzaSaleWithProductsResult>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<CreateBzaSaleWithProductsResult> Handle(CreateBzaSaleWithProductsCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar que el Evento de Venta exista
        var saleEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == request.BzaEventId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // 2. Validar que el Cliente exista
        var customer = await _context.Customers
            .Include(c => c.Collector)
                .ThenInclude(col => col.CollectorGroup)
            .FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 2.1 Validar que el recolector del cliente (y su grupo) estén activos.
        EnsureCollectorActive(customer);

        // 3. Obtener o crear la Venta (una venta por par cliente-evento)
        var sale = await _context.Sales
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.BzaEventId == request.BzaEventId && s.BzaCustomerId == request.BzaCustomerId, cancellationToken);

        if (sale is not null && sale.IsClosed)
            throw new InvalidOperationException("La venta está cerrada porque el cliente ya envió su comprobante. No se pueden agregar más productos.");

        if (sale is null)
        {
            sale = new BzaSale
            {
                BzaEventId = request.BzaEventId,
                BzaCustomerId = request.BzaCustomerId
            };
            _context.Sales.Add(sale);
        }

        // 4. Agregar los productos a la venta
        foreach (var item in request.Products)
        {
            sale.Products.Add(new BzaSoldProduct
            {
                Description = item.Description,
                Price = item.Price
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Calcular total de la venta (no se persiste)
        var total = sale.Products.Sum(p => p.Price);

        // 6. Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SaleWithProductsCreated",
            SaleId = sale.Id,
            SaleEventId = saleEvent.Id,
            SaleEventDescription = saleEvent.Description,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            ProductsAdded = request.Products.Count,
            Total = total,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return new CreateBzaSaleWithProductsResult
        {
            SaleId = sale.Id,
            ProductsAdded = request.Products.Count,
            Total = total
        };
    }

    /// <summary>
    /// Impide registrar la venta si el recolector del cliente o su grupo están inactivos.
    /// </summary>
    private static void EnsureCollectorActive(BzaCustomer customer)
    {
        var col = customer.Collector;
        if (col is null) return;

        var grp = col.CollectorGroup;
        var groupInactive = grp is not null && !grp.IsActive;

        if (!col.IsActive)
        {
            throw new SaleCollectorInactiveException(
                $"El recolector '{col.Name}' está inactivo. No se puede registrar la venta hasta reactivarlo.",
                "COLLECTOR_INACTIVE", col.Id, col.Name, true, grp?.Id, grp?.Description, groupInactive);
        }

        if (groupInactive)
        {
            throw new SaleCollectorInactiveException(
                $"El grupo de recolección '{grp!.Description}' está inactivo. No se puede registrar la venta hasta reactivarlo.",
                "COLLECTOR_GROUP_INACTIVE", col.Id, col.Name, false, grp.Id, grp.Description, true);
        }
    }
}
