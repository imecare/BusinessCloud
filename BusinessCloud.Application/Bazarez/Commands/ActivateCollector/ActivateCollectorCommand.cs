using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.ActivateCollector;

public record ActivateCollectorCommand(int Id) : IRequest;

public class ActivateCollectorHandler(IBazaresDbContext context) 
    : IRequestHandler<ActivateCollectorCommand>
{
    public async Task Handle(ActivateCollectorCommand request, CancellationToken ct)
    {
        var entity = await context.Collectors
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Recolector con Id {request.Id} no encontrado");

        entity.IsActive = true;
        await context.SaveChangesAsync(ct);
    }
}
