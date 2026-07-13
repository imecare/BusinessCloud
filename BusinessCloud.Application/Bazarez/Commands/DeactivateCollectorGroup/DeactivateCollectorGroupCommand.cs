using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.DeactivateCollectorGroup;

/// <summary>
/// Desactiva un grupo de recolección para que no aparezca en altas de recolectores
/// ni en las ventas. Requiere que TODOS sus recolectores estén desactivados.
/// </summary>
public record DeactivateCollectorGroupCommand(int Id) : IRequest;

public class DeactivateCollectorGroupHandler(IBazaresDbContext context)
    : IRequestHandler<DeactivateCollectorGroupCommand>
{
    public async Task Handle(DeactivateCollectorGroupCommand request, CancellationToken ct)
    {
        var entity = await context.CollectorGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Grupo de recolección con Id {request.Id} no encontrado");

        var hasActiveCollectors = await context.Collectors
            .AnyAsync(c => c.BzaCollectorGroupId == request.Id && c.IsActive, ct);

        if (hasActiveCollectors)
        {
            throw new InvalidOperationException(
                "Para desactivar el grupo, primero desactiva todos sus recolectores.");
        }

        entity.IsActive = false;
        await context.SaveChangesAsync(ct);
    }
}
