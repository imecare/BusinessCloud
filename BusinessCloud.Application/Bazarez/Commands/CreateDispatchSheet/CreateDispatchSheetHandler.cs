using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CreateDispatchSheet;

public class CreateDispatchSheetHandler(IBazaresDbContext context, IMongoContext mongoContext)
    : IRequestHandler<CreateDispatchSheetCommand, DispatchSheetResultDto>
{
    private readonly IBazaresDbContext _context = context;
    private readonly IMongoContext _mongoContext = mongoContext;

    public async Task<DispatchSheetResultDto> Handle(CreateDispatchSheetCommand request, CancellationToken ct)
    {
        var collector = await _context.Collectors
            .FirstOrDefaultAsync(c => c.Id == request.BzaCollectorId, ct)
            ?? throw new KeyNotFoundException("Recolector no encontrado.");

        // Ventas listas para entrega (status=3) de clientes asignados a este recolector
        var readySales = await _context.Events
            .Include(s => s.Sales).ThenInclude(x => x.Customer)
            .Include(s => s.Sales).ThenInclude(x => x.Products)
            .Where(s => s.Status == 3 && s.Sales.Any(x => x.Customer.BzaCollectorId == request.BzaCollectorId))
            .ToListAsync(ct);

        if (readySales.Count == 0)
            throw new InvalidOperationException("No hay ventas listas para entrega para este recolector.");

        var sheet = new BzaDispatchSheet
        {
            BzaCollectorId = request.BzaCollectorId,
            DispatchDate = request.DispatchDate,
            TotalPackages = readySales.Count,
            Status = 1,
            Items = readySales.Select(s => new BzaDispatchItem
            {
                BzaEventId = s.Id,
                PieceCount = s.Sales.SelectMany(x => x.Products).Count(),
                LabelCode = Guid.NewGuid().ToString("N")[..8].ToUpper()
            }).ToList()
        };

        _context.DispatchSheets.Add(sheet);

        // Actualizar estado de ventas a "Finalizado"
        foreach (var sale in readySales)
        {
            sale.Status = 4;
        }

        await _context.SaveChangesAsync(ct);

        await _mongoContext.InsertAuditLogAsync(new
        {
            Event = "Bza_DispatchCreated",
            SheetId = sheet.Id,
            CollectorName = collector.Name,
            Packages = sheet.TotalPackages,
            Timestamp = DateTime.UtcNow
        }, ct);

        return new DispatchSheetResultDto
        {
            DispatchSheetId = sheet.Id,
            CollectorName = collector.Name,
            DispatchDate = request.DispatchDate,
            TotalPackages = sheet.TotalPackages,
            Items = sheet.Items.Select(i =>
            {
                var sale = readySales.First(s => s.Id == i.BzaEventId);
                var firstCustomer = sale.Sales.FirstOrDefault()?.Customer;
                return new DispatchItemDto
                {
                    SaleId = i.BzaEventId,
                    CustomerName = firstCustomer?.Name ?? "Varios Clientes",
                    PieceCount = i.PieceCount,
                    LabelCode = i.LabelCode ?? "",
                    Total = sale.Sales.SelectMany(x => x.Products).Sum(p => p.Price)
                };
            }).ToList()
        };
    }
}
