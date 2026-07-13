using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreateCollectorGroup;

public record CreateCollectorGroupCommand(string Description, int? DeliveryDay = null) : IRequest<int>;

public class CreateCollectorGroupHandler(IBazaresDbContext context)
    : IRequestHandler<CreateCollectorGroupCommand, int>
{
    public async Task<int> Handle(CreateCollectorGroupCommand request, CancellationToken ct)
    {
        var description = request.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("El nombre del grupo es obligatorio.");

        var exists = await context.CollectorGroups
            .AnyAsync(g => g.Description.ToLower() == description.ToLower(), ct);
        if (exists)
            throw new InvalidOperationException($"Ya existe un grupo con el nombre \"{description}\".");

        var entity = new BzaCollectorGroup
        {
            Description = description,
            DeliveryDay = request.DeliveryDay
        };

        context.CollectorGroups.Add(entity);
        await context.SaveChangesAsync(ct);

        return entity.Id;
    }
}
