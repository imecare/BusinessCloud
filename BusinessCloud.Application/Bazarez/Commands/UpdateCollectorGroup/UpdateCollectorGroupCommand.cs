using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.UpdateCollectorGroup;

public record UpdateCollectorGroupCommand(int Id, string Description) : IRequest;

public class UpdateCollectorGroupHandler(IBazaresDbContext context) 
    : IRequestHandler<UpdateCollectorGroupCommand>
{
    public async Task Handle(UpdateCollectorGroupCommand request, CancellationToken ct)
    {
        var entity = await context.CollectorGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Grupo de recolección con Id {request.Id} no encontrado");

        entity.Description = request.Description;
        await context.SaveChangesAsync(ct);
    }
}
