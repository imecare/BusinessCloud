using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetPackages;

/// <summary>Lista los paquetes del catálogo, opcionalmente filtrados por módulo/sistema.</summary>
public record GetPackagesQuery(string? Module = null, bool? OnlyActive = null)
    : IRequest<IReadOnlyList<PackageDto>>;

public class GetPackagesHandler(IIdentityDbContext context)
    : IRequestHandler<GetPackagesQuery, IReadOnlyList<PackageDto>>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<IReadOnlyList<PackageDto>> Handle(
        GetPackagesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Packages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(p => p.Module == request.Module);

        if (request.OnlyActive == true)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.Module).ThenBy(p => p.Name)
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
