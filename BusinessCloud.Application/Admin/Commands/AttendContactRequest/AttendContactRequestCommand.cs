using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Admin.Commands.AttendContactRequest;

/// <summary>Marca una solicitud de contacto como atendida.</summary>
public record AttendContactRequestCommand(int Id) : IRequest<Unit>;

public class AttendContactRequestHandler(
    IIdentityDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<AttendContactRequestCommand, Unit>
{
    private readonly IIdentityDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<Unit> Handle(AttendContactRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ContactRequests
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("La solicitud no existe.");

        entity.Status = RequestStatus.Attended;
        entity.AttendedAt = DateTime.UtcNow;
        entity.AttendedBy = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
