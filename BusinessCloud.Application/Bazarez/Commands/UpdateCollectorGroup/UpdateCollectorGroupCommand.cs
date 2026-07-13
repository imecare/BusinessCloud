using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.UpdateCollectorGroup;

public record UpdateCollectorGroupCommand(int Id, string Description, int? DeliveryDay = null) : IRequest;

public class UpdateCollectorGroupHandler(IBazaresDbContext context)
    : IRequestHandler<UpdateCollectorGroupCommand>
{
    public async Task Handle(UpdateCollectorGroupCommand request, CancellationToken ct)
    {
        var description = request.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("El nombre del grupo es obligatorio.");

        var entity = await context.CollectorGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Grupo de recolección con Id {request.Id} no encontrado");

        var duplicate = await context.CollectorGroups
            .AnyAsync(g => g.Id != request.Id && g.Description.ToLower() == description.ToLower(), ct);
        if (duplicate)
            throw new InvalidOperationException($"Ya existe un grupo con el nombre \"{description}\".");

        entity.Description = description;
        entity.DeliveryDay = request.DeliveryDay;
        await context.SaveChangesAsync(ct);
    }
}
