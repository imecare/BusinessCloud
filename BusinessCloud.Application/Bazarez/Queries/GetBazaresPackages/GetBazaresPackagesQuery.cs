using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetBazaresPackages;

/// <summary>Paquetes activos del módulo Bazares que un bazar puede solicitar contratar.</summary>
public record GetBazaresPackagesQuery : IRequest<IReadOnlyList<PackageDto>>;

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
            .Where(p => p.IsActive && p.Module == SystemModules.Bazares)
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
            })
            .ToListAsync(cancellationToken);
    }
}
