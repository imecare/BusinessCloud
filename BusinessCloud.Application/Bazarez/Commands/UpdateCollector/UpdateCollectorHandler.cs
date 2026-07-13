using MediatR;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Commands.CreateCollector;

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
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del recolector es obligatorio.");

        var entity = await _context.Collectors
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            throw new KeyNotFoundException($"El recolector con ID {request.Id} no existe.");
        }

        // Mismas reglas de nombre único (case-insensitive), excluyendo al propio recolector.
        await CreateCollectorHandler.EnsureNameAllowedAsync(_context, name, request.BzaCollectorGroupId,
            request.Id, request.AllowDuplicateNameInOtherGroup, cancellationToken);

        entity.Name = name;
        entity.FacebookName = request.FacebookName;
        entity.BzaCollectorGroupId = request.BzaCollectorGroupId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}