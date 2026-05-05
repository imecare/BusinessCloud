using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.UpdateSellerStatus;

public class UpdateSellerStatusHandler : IRequestHandler<UpdateSellerStatusCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public UpdateSellerStatusHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateSellerStatusCommand request, CancellationToken cancellationToken)
    {
        var seller = await _db.Sellers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (seller is null)
            return false;

        seller.StatusId = request.StatusId;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}