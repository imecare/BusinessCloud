using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.SetCompanyModules;

public class SetCompanyModulesHandler(
    IIdentityDbContext context,
    IValidator<SetCompanyModulesCommand> validator)
    : IRequestHandler<SetCompanyModulesCommand, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(
        SetCompanyModulesCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        if (!await context.Tenants.AnyAsync(t => t.Id == request.TenantId, cancellationToken))
            throw new KeyNotFoundException($"La empresa '{request.TenantId}' no existe.");

        var selected = request.Modules.ToHashSet(StringComparer.Ordinal);
        var existing = await context.TenantModules
            .Where(m => m.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var module in existing)
        {
            var isActive = selected.Contains(module.Module);
            if (module.IsActive == isActive)
                continue;

            module.IsActive = isActive;
            if (isActive)
            {
                module.ActivatedAt = now;
                module.DeactivatedAt = null;
            }
            else
            {
                module.DeactivatedAt = now;
            }
        }

        foreach (var module in selected.Where(name => !existing.Any(m => m.Module == name)))
        {
            context.TenantModules.Add(new TenantModule
            {
                TenantId = request.TenantId,
                Module = module,
                IsActive = true,
                ActivatedAt = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return SystemModules.All.Where(selected.Contains).ToArray();
    }
}
