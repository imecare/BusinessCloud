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
        { 5, "Cancelado" },
        { 6, "Entregado" }
    };

    public async Task<List<BzaSaleListDto>> Handle(GetAllBzaSalesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Events.AsQueryable();

        // Filtro por estado
        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }

        // Filtro por rango de fechas (sobre la fecha de creación del evento), inclusivo
        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.Date;
            query = query.Where(s => s.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(s => s.CreatedAt < toExclusive);
        }

        // Filtro por búsqueda en la descripción
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s => EF.Functions.Like(s.Description, $"%{term}%"));
        }

        // Materializar datos primero para evitar memory leak en proyección con Dictionary
        var rawSales = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.Description,
                s.PaymentDeadline,
                s.Status,
                TotalEventSales = s.Sales.SelectMany(x => x.Products).Sum(p => (decimal?)p.Price) ?? 0m,
                UnsentSalesAmount = s.Sales.Where(x => x.BzaClosureEventId == null).SelectMany(x => x.Products).Sum(p => (decimal?)p.Price) ?? 0m,
                HasSentSales = s.Sales.Any(x => x.BzaClosureEventId != null),
                UniqueCustomersCount = s.Sales.Select(x => x.BzaCustomerId).Distinct().Count(),
                TotalPaid = s.Payments.Where(p => p.IsVerified).Sum(p => (decimal?)p.Amount) ?? 0m,
                s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Mapear StatusName en memoria (evita EF Core memory leak warning)
        return rawSales.Select(s => new BzaSaleListDto
        {
            Id = s.Id,
            Description = s.Description,
            PaymentDeadline = s.PaymentDeadline,
            Status = s.Status,
            StatusName = StatusNames.GetValueOrDefault(s.Status, "Desconocido"),
            TotalEventSales = s.TotalEventSales,
            UnsentSalesAmount = s.UnsentSalesAmount,
            HasSentSales = s.HasSentSales,
            UniqueCustomersCount = s.UniqueCustomersCount,
            TotalCustomers = s.UniqueCustomersCount,
            TotalAmount = s.TotalEventSales,
            TotalPaid = s.TotalPaid,
            TotalPending = Math.Max(0m, s.TotalEventSales - s.TotalPaid),
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}
