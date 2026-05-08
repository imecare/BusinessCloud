using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.MarkSaleReadyForDelivery;

public class MarkSaleReadyForDeliveryHandler : IRequestHandler<MarkSaleReadyForDeliveryCommand>
{
    private readonly IBazaresDbContext _context;

    public MarkSaleReadyForDeliveryHandler(IBazaresDbContext context) => _context = context;

    public async Task Handle(MarkSaleReadyForDeliveryCommand request, CancellationToken ct)
    {
        var sale = await _context.Sales
            .FirstOrDefaultAsync(s => s.Id == request.BzaSaleId, ct)
            ?? throw new KeyNotFoundException("Venta no encontrada.");

        if (sale.Status != 2)
            throw new InvalidOperationException("Solo ventas pagadas pueden marcarse como listas para entrega.");

        sale.Status = 3; // Listo para Entrega
        await _context.SaveChangesAsync(ct);
    }
}
