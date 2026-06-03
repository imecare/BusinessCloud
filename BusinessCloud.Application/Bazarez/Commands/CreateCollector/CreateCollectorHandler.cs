using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreateCollector;

public class CreateCollectorHandler : IRequestHandler<CreateCollectorCommand, int>
{
    private readonly IBazaresDbContext _context;

    public CreateCollectorHandler(IBazaresDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateCollectorCommand request, CancellationToken cancellationToken)
    {
        var entity = new BzaCollector
        {
            Name = request.Name,
            FacebookName = request.FacebookName,
            BzaCollectorGroupId = request.BzaCollectorGroupId
        };

        _context.Collectors.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}