using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.UpdateSeller;

public class UpdateSellerHandler : IRequestHandler<UpdateSellerCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public UpdateSellerHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateSellerCommand request, CancellationToken cancellationToken)
    {
        var seller = await _db.Sellers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (seller is null) return false;

        seller.Name = request.Name;
        seller.LastName = request.LastName;
        seller.Phone = request.Phone;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
