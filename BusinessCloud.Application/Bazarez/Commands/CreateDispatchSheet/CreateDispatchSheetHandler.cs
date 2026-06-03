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
        var readySales = await _context.Sales
            .Include(s => s.SoldProducts).ThenInclude(p => p.Customer)
            .Where(s => s.Status == 3 && s.SoldProducts.Any(p => p.Customer.BzaCollectorId == request.BzaCollectorId))
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
                PieceCount = s.SoldProducts.Count,
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
                var sale = readySales.First(s => s.Id == i.BzaSaleId);
                var firstCustomer = sale.SoldProducts.FirstOrDefault()?.Customer;
                return new DispatchItemDto
                {
                    SaleId = i.BzaSaleId,
                    CustomerName = firstCustomer?.Name ?? "Varios Clientes",
                    PieceCount = i.PieceCount,
                    LabelCode = i.LabelCode ?? "",
                    Total = sale.SoldProducts.Sum(p => p.Price)
                };
            }).ToList()
        };
    }
}
