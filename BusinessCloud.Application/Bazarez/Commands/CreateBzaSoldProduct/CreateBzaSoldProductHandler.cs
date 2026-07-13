using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSoldProduct;

public class CreateBzaSoldProductHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<CreateBzaSoldProductCommand, int>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<int> Handle(CreateBzaSoldProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar que el Evento de Venta exista
        var saleEvent = await _context.Events.FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // 1.1 Si el evento ya está en proceso de pago (tiene ventas enviadas a cobro),
        //     no se pueden agregar más ventas/productos.
        var eventInPayment = await _context.Sales
            .AnyAsync(s => s.BzaEventId == request.BzaSaleId && s.BzaClosureEventId != null, cancellationToken);
        if (eventInPayment)
            throw new InvalidOperationException(
                "El evento ya está en proceso de pago (se enviaron totales a los clientes). No se pueden agregar más ventas a este evento.");

        // 2. Validar que el Cliente exista
        var customer = await _context.Customers
            .Include(c => c.Collector)
                .ThenInclude(col => col.CollectorGroup)
            .FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 2.1 Validar que el recolector del cliente (y su grupo) estén activos.
        EnsureCollectorActive(customer);

        // 3. Obtener o crear la Venta (Cliente + Evento). Una venta por par cliente-evento.
        var sale = await _context.Sales.FirstOrDefaultAsync(
            s => s.BzaEventId == request.BzaSaleId && s.BzaCustomerId == request.BzaCustomerId, cancellationToken);

        if (sale is not null && sale.IsClosed)
            throw new InvalidOperationException("La venta está cerrada porque el cliente ya envió su comprobante. No se pueden agregar más productos.");

        if (sale is null)
        {
            sale = new BzaSale
            {
                BzaEventId = request.BzaSaleId,
                BzaCustomerId = request.BzaCustomerId
            };
            _context.Sales.Add(sale);
        }

        // 4. Crear el producto y asociarlo a la venta
        var soldProduct = new BzaSoldProduct
        {
            Sale = sale,
            Description = request.Description,
            Price = request.Price
        };

        _context.SoldProducts.Add(soldProduct);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SoldProductCreated",
            SoldProductId = soldProduct.Id,
            SaleEventId = saleEvent.Id,
            SaleEventDescription = saleEvent.Description,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Price = soldProduct.Price,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return soldProduct.Id;
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
