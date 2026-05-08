using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.CreateDispatchSheet;

public class CreateDispatchSheetHandler : IRequestHandler<CreateDispatchSheetCommand, DispatchSheetResultDto>
{
    private readonly IBazaresDbContext _context;
    private readonly IMongoContext _mongoContext;

    public CreateDispatchSheetHandler(IBazaresDbContext context, IMongoContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public async Task<DispatchSheetResultDto> Handle(CreateDispatchSheetCommand request, CancellationToken ct)
    {
        var collector = await _context.Collectors
            .FirstOrDefaultAsync(c => c.Id == request.BzaCollectorId, ct)
            ?? throw new KeyNotFoundException("Recolector no encontrado.");

        // Ventas pagadas (status=2) de clientes asignados a este recolector, aún no entregadas
        var readySales = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Products)
            .Where(s => s.Customer.BzaCollectorId == request.BzaCollectorId
                     && s.Status == 3) // Listo para Entrega
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
                BzaSaleId = s.Id,
                PieceCount = s.Products.Count,
                LabelCode = s.LabelCode ?? Guid.NewGuid().ToString("N")[..8].ToUpper()
            }).ToList()
        };

        _context.DispatchSheets.Add(sheet);

        // Actualizar estado de ventas a "Entregado a Recolector"
        foreach (var sale in readySales)
        {
            sale.Status = 4;
            sale.DeliveredToCollectorAt = DateTime.UtcNow;
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
                var sale = readySales.First(s => s.Id == i.BzaSaleId);
                return new DispatchItemDto
                {
                    SaleId = i.BzaSaleId,
                    CustomerName = sale.Customer.Name,
                    PieceCount = i.PieceCount,
                    LabelCode = i.LabelCode ?? "",
                    Total = sale.Total
                };
            }).ToList()
        };
    }
}
