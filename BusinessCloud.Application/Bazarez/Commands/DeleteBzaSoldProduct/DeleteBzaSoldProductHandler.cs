using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeleteBzaSoldProduct;

public class DeleteBzaSoldProductHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<DeleteBzaSoldProductCommand, bool>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<bool> Handle(DeleteBzaSoldProductCommand request, CancellationToken cancellationToken)
    {
        var soldProduct = await _context.SoldProducts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (soldProduct is null) return false;

        var saleEventId = soldProduct.BzaSaleId;
        var customerId = soldProduct.BzaCustomerId;
        var description = soldProduct.Description;
        var price = soldProduct.Price;

        _context.SoldProducts.Remove(soldProduct);
        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SoldProductDeleted",
            SoldProductId = request.Id,
            SaleEventId = saleEventId,
            CustomerId = customerId,
            Description = description,
            Price = price,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
