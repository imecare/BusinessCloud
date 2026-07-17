using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Queries.GetMessageRequests;

/// <summary>Lista las solicitudes de paquetes de mensajes, opcionalmente por estado.</summary>
public record GetMessageRequestsQuery(string? Status = null)
    : IRequest<IReadOnlyList<MessageRequestDto>>;

public class GetMessageRequestsHandler(IIdentityDbContext context)
    : IRequestHandler<GetMessageRequestsQuery, IReadOnlyList<MessageRequestDto>>
{
    private readonly IIdentityDbContext _context = context;

    public async Task<IReadOnlyList<MessageRequestDto>> Handle(
        GetMessageRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.MessagePackageRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(r => r.Status == request.Status);

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new MessageRequestDto
            {
                Id = r.Id,
                TenantId = r.TenantId,
                CompanyName = r.CompanyName,
                PackageId = r.PackageId,
                PackageName = r.PackageName,
                RequestedMessages = r.RequestedMessages,
                Price = r.Price,
                Status = r.Status,
                RequestedByName = r.RequestedByName,
                Note = r.Note,
                RequestedAt = r.RequestedAt,
                AttendedAt = r.AttendedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
