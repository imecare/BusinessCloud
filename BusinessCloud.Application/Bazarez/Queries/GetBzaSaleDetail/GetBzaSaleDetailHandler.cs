using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;

public class GetBzaSaleDetailHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<GetBzaSaleDetailQuery, BzaSaleDetailDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    private static readonly Dictionary<int, string> StatusNames = new()
    {
        { 1, "Abierto" },
        { 2, "Cerrado" },
        { 3, "En Entrega" },
        { 4, "Finalizado" },
        { 5, "Cancelado" }
    };

    public async Task<BzaSaleDetailDto> Handle(GetBzaSaleDetailQuery request, CancellationToken cancellationToken)
    {
        // 1. Consultar SQL Server (Datos relacionales)
        var saleEvent = await _context.Sales
            .Include(s => s.SoldProducts)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Evento de Venta no encontrado.");

        // 2. Calcular métricas
        var totalRevenue = saleEvent.SoldProducts.Sum(p => p.Price);
        var productsCount = saleEvent.SoldProducts.Count;
        var uniqueCustomersCount = saleEvent.SoldProducts.Select(p => p.BzaCustomerId).Distinct().Count();
        var totalPaid = saleEvent.Payments.Where(p => p.IsVerified).Sum(p => p.Amount);
        var pendingAmount = Math.Max(0, totalRevenue - totalPaid);

        // 3. Consultar MongoDB (Historial de auditoría)
        var mongoLogs = await _mongoContext.GetAuditLogsBySaleIdAsync(saleEvent.Id, cancellationToken);

        // 4. Mapear y Retornar
        return new BzaSaleDetailDto
        {
            Id = saleEvent.Id,
            Description = saleEvent.Description,
            PaymentDeadline = saleEvent.PaymentDeadline,
            DeliveryDate = saleEvent.DeliveryDate,
            Status = saleEvent.Status,
            StatusName = StatusNames.GetValueOrDefault(saleEvent.Status, "Desconocido"),
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
}