using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSoldProduct;

public class UpdateBzaSoldProductHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<UpdateBzaSoldProductCommand, bool>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<bool> Handle(UpdateBzaSoldProductCommand request, CancellationToken cancellationToken)
    {
        var soldProduct = await _context.SoldProducts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (soldProduct is null) return false;

        var oldPrice = soldProduct.Price;

        soldProduct.Description = request.Description;
        soldProduct.Price = request.Price;

        await _context.SaveChangesAsync(cancellationToken);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_SoldProductUpdated",
            SoldProductId = soldProduct.Id,
            SaleEventId = soldProduct.BzaSaleId,
            CustomerId = soldProduct.BzaCustomerId,
            OldPrice = oldPrice,
            NewPrice = request.Price,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
