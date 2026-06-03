using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaProduct;

public class CreateBzaProductHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<CreateBzaProductCommand, int>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<int> Handle(CreateBzaProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar que el Evento de Venta exista
        var saleEvent = await _context.Sales.FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // 2. Validar que el Cliente exista
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 3. Crear el producto/compra
        var product = new BzaProduct
        {
            BzaSaleId = request.BzaSaleId,
            BzaCustomerId = request.BzaCustomerId,
            Description = request.Description,
            Price = request.Price
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Auditoría en MongoDB
        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_ProductCreated",
            ProductId = product.Id,
            SaleEventId = saleEvent.Id,
            SaleEventDescription = saleEvent.Description,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Price = product.Price,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return product.Id;
    }
}
