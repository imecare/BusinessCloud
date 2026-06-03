using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreateCollectorGroup;

public record CreateCollectorGroupCommand(string Description) : IRequest<int>;

public class CreateCollectorGroupHandler(IBazaresDbContext context) 
    : IRequestHandler<CreateCollectorGroupCommand, int>
{
    public async Task<int> Handle(CreateCollectorGroupCommand request, CancellationToken ct)
    {
        var entity = new BzaCollectorGroup
        {
            Description = request.Description
        };

        context.CollectorGroups.Add(entity);
        await context.SaveChangesAsync(ct);

        return entity.Id;
    }
}
