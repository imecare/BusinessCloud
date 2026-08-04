using BusinessCloud.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Queries.GetGlobalCollectorGroups;

public class GetGlobalCollectorGroupsHandler(
    IBazaresDbContext context,
    ICurrentUserService currentUser,
    IValidator<GetGlobalCollectorGroupsQuery> validator)
    : IRequestHandler<GetGlobalCollectorGroupsQuery, IReadOnlyList<GlobalCollectorGroupDto>>
{
    public async Task<IReadOnlyList<GlobalCollectorGroupDto>> Handle(
        GetGlobalCollectorGroupsQuery request,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        currentUser.GetRequiredTenantId();

        return await context.GlobalCollectorGroups
            .AsNoTracking()
            .OrderBy(group => group.Description)
            .Select(group => new GlobalCollectorGroupDto(
                group.Id,
                group.Description,
                group.DeliveryFrequency,
                group.DeliveryDay,
                group.Collectors.Count,
                group.Collectors
                    .OrderBy(collector => collector.Name)
                    .Select(collector => new GlobalCollectorDto(collector.Id, collector.Name))
                    .ToList()))
            .ToListAsync(ct);
    }
}
