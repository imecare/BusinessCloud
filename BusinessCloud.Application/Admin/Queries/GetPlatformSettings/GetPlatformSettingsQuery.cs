using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetPlatformSettings;

/// <summary>Obtiene los ajustes de la plataforma (crea la fila por defecto si no existe).</summary>
public record GetPlatformSettingsQuery : IRequest<PlatformSettingsDto>;

public class GetPlatformSettingsHandler(IIdentityDbContext context)
    : IRequestHandler<GetPlatformSettingsQuery, PlatformSettingsDto>
{
    private const string DefaultSuperAdminPhone = "3121232192";
    private readonly IIdentityDbContext _context = context;

    public async Task<PlatformSettingsDto> Handle(
        GetPlatformSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _context.PlatformSettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new PlatformSettings { Id = 1, SuperAdminPhone = DefaultSuperAdminPhone };
            _context.PlatformSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new PlatformSettingsDto { SuperAdminPhone = settings.SuperAdminPhone };
    }
}
