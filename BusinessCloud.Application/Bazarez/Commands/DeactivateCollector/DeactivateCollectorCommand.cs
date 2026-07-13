using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.DeactivateCollector;

/// <summary>
/// Desactiva un recolector para que no aparezca en las altas de clientes ni en las ventas.
/// No se elimina: conserva su historial y clientes relacionados.
/// </summary>
public record DeactivateCollectorCommand(int Id) : IRequest;

public class DeactivateCollectorHandler(IBazaresDbContext context)
    : IRequestHandler<DeactivateCollectorCommand>
{
    public async Task Handle(DeactivateCollectorCommand request, CancellationToken ct)
    {
        var entity = await context.Collectors
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Recolector con Id {request.Id} no encontrado");

        entity.IsActive = false;
        await context.SaveChangesAsync(ct);
    }
}
