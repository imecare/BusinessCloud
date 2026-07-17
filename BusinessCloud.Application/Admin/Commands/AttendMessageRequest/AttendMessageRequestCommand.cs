using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.AttendMessageRequest;

/// <summary>Marca una solicitud de paquete de mensajes como atendida (o rechazada).</summary>
public record AttendMessageRequestCommand(int Id, bool Reject = false) : IRequest<Unit>;

public class AttendMessageRequestHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<AttendMessageRequestCommand, Unit>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<Unit> Handle(AttendMessageRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MessagePackageRequests
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("La solicitud no existe.");

        entity.Status = request.Reject ? RequestStatus.Rejected : RequestStatus.Attended;
        entity.AttendedAt = DateTime.UtcNow;
        entity.AttendedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
