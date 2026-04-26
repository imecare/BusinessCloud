using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Company.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Company.Queries.GetCompanyContext;

public class GetCompanyContextQueryHandler : IRequestHandler<GetCompanyContextQuery, CompanyContextDto?>
{
    private readonly IPaymentsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCompanyContextQueryHandler(IPaymentsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CompanyContextDto?> Handle(GetCompanyContextQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        if (string.IsNullOrEmpty(tenantId))
            return null;

        var tenant = await _db.Tenants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive, cancellationToken);

        if (tenant is null)
            return null;

        return new CompanyContextDto
        {
            CompanyName = tenant.Name,
            CompanyCode = tenant.Id,
            TenantId = tenant.Id
        };
    }
}