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

        if (sale.IsCashSale)
        {
            if (request.TotalAmount != sale.TotalAmount ||
                request.Date.HasValue && request.Date.Value != sale.Date)
                throw new InvalidOperationException("No se puede cambiar el total ni la fecha de una venta de contado cobrada.");
            if (!request.SellerId.HasValue ||
                !await _db.Sellers.AnyAsync(s => s.Id == request.SellerId, cancellationToken))
                throw new ArgumentException("La venta de contado requiere un comisionista de esta empresa.");
        }
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new ArgumentException("El cliente no existe en esta empresa.");
        if (customer.IsGeneralPublic && !sale.IsCashSale)
            throw new ArgumentException("Publico en general solo admite ventas de contado.");

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
