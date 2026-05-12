using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Commands.UpdateSale;

public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, bool>
{
    private readonly IPaymentsDbContext _db;

    public UpdateSaleHandler(IPaymentsDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await _db.Sales
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (sale is null) return false;

        sale.CustomerId = request.CustomerId;
        sale.SellerId = request.SellerId;
        sale.TotalAmount = request.TotalAmount;
        sale.CostPrice = request.CostPrice;
        sale.CommissionAmount = request.CommissionAmount;
        sale.ProductDescription = request.ProductDescription;
        if (request.Date.HasValue)
            sale.Date = request.Date.Value;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
