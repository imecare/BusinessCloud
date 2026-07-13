using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.DeleteCollector;

public record DeleteCollectorCommand(int Id) : IRequest;

public class DeleteCollectorHandler(IBazaresDbContext context)
    : IRequestHandler<DeleteCollectorCommand>
{
    public async Task Handle(DeleteCollectorCommand request, CancellationToken ct)
    {
        var entity = await context.Collectors
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Recolector con Id {request.Id} no encontrado");

        // Verificar dependencias
        var hasCustomers = await context.Customers
            .AnyAsync(c => c.BzaCollectorId == request.Id, ct);

        var hasDispatchSheets = await context.DispatchSheets
            .AnyAsync(d => d.BzaCollectorId == request.Id, ct);

        if (hasCustomers)
        {
            // No se puede eliminar un recolector con clientes relacionados. Debe desactivarse.
            throw new InvalidOperationException(
                "No se puede eliminar un recolector que tiene clientes relacionados. Reasigna sus clientes o desactívalo.");
        }

        if (hasDispatchSheets)
        {
            throw new InvalidOperationException(
                "No se puede eliminar un recolector con hojas de despacho asociadas. Desactívalo en su lugar.");
        }

        // Solo se elimina físicamente si no tiene ninguna dependencia.
        context.Collectors.Remove(entity);
        await context.SaveChangesAsync(ct);
    }
}
