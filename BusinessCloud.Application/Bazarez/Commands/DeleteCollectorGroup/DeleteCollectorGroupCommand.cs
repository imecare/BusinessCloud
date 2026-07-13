using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.DeleteCollectorGroup;

public record DeleteCollectorGroupCommand(int Id) : IRequest;

public class DeleteCollectorGroupHandler(IBazaresDbContext context)
    : IRequestHandler<DeleteCollectorGroupCommand>
{
    public async Task Handle(DeleteCollectorGroupCommand request, CancellationToken ct)
    {
        var entity = await context.CollectorGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Grupo de recolección con Id {request.Id} no encontrado");

        var hasCollectors = await context.Collectors
            .AnyAsync(c => c.BzaCollectorGroupId == request.Id, ct);

        if (hasCollectors)
        {
            // No se puede eliminar un grupo con recolectores. Debe desactivarse.
            throw new InvalidOperationException(
                "No se puede eliminar un grupo que tiene recolectores. Reasigna o elimina sus recolectores, o desactiva el grupo.");
        }

        // Solo se elimina físicamente si no tiene ningún recolector.
        context.CollectorGroups.Remove(entity);
        await context.SaveChangesAsync(ct);
    }
}
