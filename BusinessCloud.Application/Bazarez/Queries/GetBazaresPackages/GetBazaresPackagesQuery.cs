using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBazaresPackages;

/// <summary>Paquetes activos del módulo Bazares que un bazar puede solicitar contratar.</summary>
/// <param name="OnlyExtra">
/// false = solo paquetes mensuales (plan normal); true = solo paquetes extra de transacciones
/// (recargas puntuales que se ofrecen cuando quedan pocas transacciones).
/// </param>
public record GetBazaresPackagesQuery(bool OnlyExtra = false) : IRequest<IReadOnlyList<PackageDto>>;

public class GetBazaresPackagesHandler(IIdentityDbContext context)
    : IRequestHandler<GetBazaresPackagesQuery, IReadOnlyList<PackageDto>>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<IReadOnlyList<PackageDto>> Handle(
        GetBazaresPackagesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Packages
            .AsNoTracking()
            .Where(p => p.IsActive && p.Module == SystemModules.Bazares && p.IsExtra == request.OnlyExtra)
            .OrderBy(p => p.Price)
            .Select(p => new PackageDto
            {
                Id = p.Id,
                Name = p.Name,
                Module = p.Module,
                Price = p.Price,
                Currency = p.Currency,
                IncludedMessages = p.IncludedMessages,
                IsActive = p.IsActive,
                Description = p.Description,
                IsExtra = p.IsExtra,
            })
            .ToListAsync(cancellationToken);
    }
}
