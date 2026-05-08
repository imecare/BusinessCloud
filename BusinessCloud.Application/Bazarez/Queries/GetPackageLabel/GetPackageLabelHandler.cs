using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetPackageLabel;

public class GetPackageLabelHandler : IRequestHandler<GetPackageLabelQuery, PackageLabelDto>
{
    private readonly IBazaresDbContext _context;

    public GetPackageLabelHandler(IBazaresDbContext context) => _context = context;

    public async Task<PackageLabelDto> Handle(GetPackageLabelQuery request, CancellationToken ct)
    {
        var sale = await _context.Sales
            .Include(s => s.Customer).ThenInclude(c => c.Collector)
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, ct)
            ?? throw new KeyNotFoundException("Venta no encontrada.");

        if (string.IsNullOrEmpty(sale.LabelCode))
        {
            sale.LabelCode = Guid.NewGuid().ToString("N")[..8].ToUpper();
            await _context.SaveChangesAsync(ct);
        }

        return new PackageLabelDto
        {
            SaleId = sale.Id,
            CustomerName = sale.Customer.Name,
            CollectorName = sale.Customer.Collector.Name,
            CollectorGroup = sale.Customer.Collector.GroupId,
            LabelCode = sale.LabelCode,
            PieceCount = sale.Products.Count,
            Total = sale.Total,
            Date = sale.CreatedAt,
            Products = sale.Products.Select(p => p.Description).ToList()
        };
    }
}
