using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;

public class GetBzaSaleDetailHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<GetBzaSaleDetailQuery, BzaSaleDetailDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Abierto" },
        { 2, "En proceso de pago" },
        { 3, "En Entrega" },
        { 4, "Finalizado" },
        { 5, "Cancelado" }
    };

    public async Task<BzaSaleDetailDto> Handle(GetBzaSaleDetailQuery request, CancellationToken cancellationToken)
    {
        var saleEvent = await _context.Events
            .Include(s => s.Sales).ThenInclude(s => s.Products)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        var closure = await _context.ClosureEventItems
            .AsNoTracking()
            .Where(i => i.BzaEventId == saleEvent.Id)
            .Join(
                _context.ClosureEvents.AsNoTracking(),
                item => item.BzaClosureEventId,
                c => c.Id,
                (item, c) => new
                {
                    c.Id,
                    c.Status,
                    c.InDeliveryProcess,
                    c.Delivered,
                    c.CreatedAt
                })
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Select(c => new ClosureStatusSnapshot(c.Status, c.InDeliveryProcess, c.Delivered))
            .FirstOrDefaultAsync(cancellationToken);

        var effectiveStatus = ResolveEventStatus(saleEvent.Status, closure);

        var totalRevenue = saleEvent.Sales.SelectMany(s => s.Products).Sum(p => p.Price);
        var productsCount = saleEvent.Sales.SelectMany(s => s.Products).Count();
        var uniqueCustomersCount = saleEvent.Sales.Select(s => s.BzaCustomerId).Distinct().Count();
        var totalPaid = saleEvent.Payments.Where(p => p.IsVerified).Sum(p => p.Amount);
        var pendingAmount = Math.Max(0m, totalRevenue - totalPaid);
        var collectionPercentage = totalRevenue > 0
            ? Math.Round((totalPaid / totalRevenue) * 100m, 2)
            : 0m;

        var mongoLogs = await _mongoContext.GetAuditLogsBySaleIdAsync(saleEvent.Id, cancellationToken);

        return new BzaSaleDetailDto
        {
            Id = saleEvent.Id,
            Description = saleEvent.Description,
            PaymentDeadline = saleEvent.PaymentDeadline,
            Status = effectiveStatus,
            StatusName = StatusNames.GetValueOrDefault(effectiveStatus, "Desconocido"),
            Metrics = new BzaSaleMetricsDto
            {
                TotalRevenue = totalRevenue,
                ProductsCount = productsCount,
                UniqueCustomersCount = uniqueCustomersCount,
                UniqueCustomers = uniqueCustomersCount,
                TotalProducts = productsCount,
                TotalSales = totalRevenue,
                TotalPaid = totalPaid,
                PendingAmount = pendingAmount,
                TotalCollected = totalPaid,
                TotalPending = pendingAmount,
                CollectionPercentage = collectionPercentage
            },
            TotalRevenue = totalRevenue,
            ProductsCount = productsCount,
            UniqueCustomersCount = uniqueCustomersCount,
            TotalPaid = totalPaid,
            PendingAmount = pendingAmount,
            AuditHistory = mongoLogs.Select(l => new BzaSaleAuditDto
            {
                Event = l.Event ?? string.Empty,
                Timestamp = l.Timestamp,
                Details = l.Details ?? string.Empty
            }).ToList()
        };
    }

    private sealed record ClosureStatusSnapshot(int Status, bool InDeliveryProcess, bool Delivered);

    private static int ResolveEventStatus(int currentStatus, ClosureStatusSnapshot? closure)
    {
        if (closure is null)
        {
            return currentStatus == 6 ? 4 : currentStatus;
        }

        if (closure.Status == BzaClosureEventStatus.Cancelled)
        {
            return 5;
        }

        if (closure.Delivered)
        {
            return 4;
        }

        if (closure.InDeliveryProcess)
        {
            return 3;
        }

        return 2;
    }
}


