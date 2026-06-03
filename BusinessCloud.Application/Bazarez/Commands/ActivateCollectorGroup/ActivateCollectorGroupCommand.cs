using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.ActivateCollectorGroup;

public record ActivateCollectorGroupCommand(int Id) : IRequest;

public class ActivateCollectorGroupHandler(IBazaresDbContext context) 
    : IRequestHandler<ActivateCollectorGroupCommand>
{
    public async Task Handle(ActivateCollectorGroupCommand request, CancellationToken ct)
    {
        var entity = await context.CollectorGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Grupo de recolección con Id {request.Id} no encontrado");

        entity.IsActive = true;
        await context.SaveChangesAsync(ct);
    }
}
