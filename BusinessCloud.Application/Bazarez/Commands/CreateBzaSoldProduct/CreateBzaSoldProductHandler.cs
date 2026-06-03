using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
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
        var saleEvent = await _context.Sales.FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // 2. Validar que el Cliente exista
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.BzaCustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 3. Crear el registro de producto vendido
        var soldProduct = new BzaSoldProduct
        {
            BzaSaleId = request.BzaSaleId,
            BzaCustomerId = request.BzaCustomerId,
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
}
