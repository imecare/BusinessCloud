using MediatR;
using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Bazares.Commands.UpdateCollector;

public class UpdateCollectorHandler : IRequestHandler<UpdateCollectorCommand>
{
    private readonly IBazaresDbContext _context;

    public UpdateCollectorHandler(IBazaresDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCollectorCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Collectors
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            // Aquí podrías lanzar tu ExceptionMiddleware personalizado
            throw new Exception($"El recolector con ID {request.Id} no existe.");
        }

        entity.Name = request.Name;
        entity.FacebookName = request.FacebookName;
        entity.GroupId = request.GroupId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}