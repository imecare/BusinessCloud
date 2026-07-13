using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using BusinessCloud.Domain.Common.Exceptions;

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
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del recolector es obligatorio.");

        await EnsureNameAllowedAsync(_context, name, request.BzaCollectorGroupId, null,
            request.AllowDuplicateNameInOtherGroup, cancellationToken);

        var entity = new BzaCollector
        {
            Name = name,
            FacebookName = request.FacebookName,
            BzaCollectorGroupId = request.BzaCollectorGroupId
        };

        _context.Collectors.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    /// <summary>
    /// Valida el nombre del recolector (sin importar mayúsculas/minúsculas):
    /// - Si ya existe con ese nombre en el MISMO grupo → no se permite (definitivo).
    /// - Si existe en OTRO grupo → se permite solo si el usuario lo confirma.
    /// </summary>
    internal static async Task EnsureNameAllowedAsync(
        IBazaresDbContext context,
        string name,
        int? targetGroupId,
        int? excludeCollectorId,
        bool allowDuplicateInOtherGroup,
        CancellationToken ct)
    {
        var lower = name.ToLower();
        var sameName = await context.Collectors
            .Include(c => c.CollectorGroup)
            .Where(c => c.Name.ToLower() == lower && (excludeCollectorId == null || c.Id != excludeCollectorId))
            .ToListAsync(ct);

        if (sameName.Count == 0) return;

        // Mismo grupo (ambos pueden ser null = Independiente) → bloqueo definitivo.
        var sameGroup = sameName.FirstOrDefault(c => c.BzaCollectorGroupId == targetGroupId);
        if (sameGroup is not null)
        {
            var grp = sameGroup.CollectorGroup?.Description ?? "Independiente";
            throw new CollectorNameConflictException(
                $"Ya existe un recolector con el nombre \"{name}\" en el grupo \"{grp}\". No se permite duplicarlo en el mismo grupo.",
                "COLLECTOR_NAME_SAME_GROUP", grp);
        }

        // Existe en otro grupo → advertir; aceptar solo bajo confirmación.
        if (!allowDuplicateInOtherGroup)
        {
            var other = sameName[0];
            var grp = other.CollectorGroup?.Description ?? "Independiente";
            throw new CollectorNameConflictException(
                $"El recolector \"{name}\" ya está dado de alta en el grupo \"{grp}\".",
                "COLLECTOR_NAME_OTHER_GROUP", grp);
        }
    }
}