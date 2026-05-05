using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Payments.Entities;
using MediatR;

namespace BusinessCloud.Application.Payments.Commands.CreateSeller;

public class CreateSellerHandler : IRequestHandler<CreateSellerCommand, int>
{
    private readonly IPaymentsDbContext _db;

    public CreateSellerHandler(IPaymentsDbContext db) => _db = db;

    public async Task<int> Handle(CreateSellerCommand request, CancellationToken cancellationToken)
    {
        var seller = new Seller
        {
            Name = request.Name,
            LastName = request.LastName,
            Phone = request.Phone,
            StatusId = 1, // Activo por defecto
            Date = DateTime.UtcNow
        };

        _db.Sellers.Add(seller);
        await _db.SaveChangesAsync(cancellationToken);

        return seller.Id;
    }
}


