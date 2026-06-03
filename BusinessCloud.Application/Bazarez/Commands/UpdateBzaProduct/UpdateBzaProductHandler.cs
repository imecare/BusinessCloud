using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaProduct;

public class UpdateBzaProductHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<UpdateBzaProductCommand, bool>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<bool> Handle(UpdateBzaProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null) return false;

        var oldPrice = product.Price;

        product.Description = request.Description;
        product.Price = request.Price;

        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_ProductUpdated",
            ProductId = product.Id,
            SaleEventId = product.BzaSaleId,
            CustomerId = product.BzaCustomerId,
            OldPrice = oldPrice,
            NewPrice = request.Price,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
