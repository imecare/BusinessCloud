using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetContactRequests;

/// <summary>Lista las solicitudes de contacto (contratar/reactivar) desde el login.</summary>
public record GetContactRequestsQuery(string? Status = null)
    : IRequest<IReadOnlyList<ContactRequestDto>>;

public class GetContactRequestsHandler(IIdentityDbContext context)
    : IRequestHandler<GetContactRequestsQuery, IReadOnlyList<ContactRequestDto>>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<IReadOnlyList<ContactRequestDto>> Handle(
        GetContactRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ContactRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ContactRequestDto
            {
                Id = r.Id,
                Phone = r.Phone,
                Type = r.Type,
                Message = r.Message,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                AttendedAt = r.AttendedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
