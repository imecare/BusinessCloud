using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetAllBzaSales;

public class GetAllBzaSalesHandler(IBazaresDbContext context)
    : IRequestHandler<GetAllBzaSalesQuery, List<BzaSaleListDto>>
{
    private readonly IBazaresDbContext _context = context;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Abierto" },
        { 2, "Cerrado" },
        { 3, "En Entrega" },
        { 4, "Finalizado" },
        { 5, "Cancelado" }
    };

    public async Task<List<BzaSaleListDto>> Handle(GetAllBzaSalesQuery request, CancellationToken cancellationToken)
    {
        var sales = await _context.Sales
            .Include(s => s.SoldProducts)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new BzaSaleListDto
            {
                Id = s.Id,
                Description = s.Description,
                PaymentDeadline = s.PaymentDeadline,
                DeliveryDate = s.DeliveryDate,
                Status = s.Status,
                StatusName = StatusNames.ContainsKey(s.Status) ? StatusNames[s.Status] : "Desconocido",
                TotalEventSales = s.SoldProducts.Sum(p => p.Price),
                UniqueCustomersCount = s.SoldProducts.Select(p => p.BzaCustomerId).Distinct().Count(),
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return sales;
    }
}
