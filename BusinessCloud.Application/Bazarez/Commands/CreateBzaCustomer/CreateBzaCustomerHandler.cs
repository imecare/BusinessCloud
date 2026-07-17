using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;

public class CreateBzaCustomerHandler : IRequestHandler<CreateBzaCustomerCommand, int>
{
    private readonly IBazaresDbContext _context;
    private readonly IVerificationCodeService _verification;
    private readonly ICurrentUserService _currentUser;

    public CreateBzaCustomerHandler(
        IBazaresDbContext context,
        IVerificationCodeService verification,
        ICurrentUserService currentUser)
    {
        _context = context;
        _verification = verification;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreateBzaCustomerCommand request, CancellationToken cancellationToken)
    {
        // El teléfono es la llave para el envío de totales: se normaliza a solo dígitos y debe ser único.
        var phone = NormalizePhone(request.Phone);
        var facebookName = FacebookMessengerProfile.Normalize(request.FacebookName);

        // Validación de lista de bloqueo: si el nombre o el Facebook coinciden con un
        // cliente bloqueado activo, no se permite el alta salvo autorización del SuperAdmin (OTP).
        var nameLower = (request.Name ?? string.Empty).Trim().ToLower();
        var fbLower = string.IsNullOrWhiteSpace(facebookName) ? null : facebookName.Trim().ToLower();

        var block = await _context.BlockedCustomers
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.IsActive && (
                b.Name.ToLower() == nameLower ||
                (fbLower != null && b.FacebookName != null && b.FacebookName.ToLower() == fbLower)),
                cancellationToken);

        if (block is not null)
        {
            var hasChallenge = !string.IsNullOrWhiteSpace(request.ChallengeId)
                               && !string.IsNullOrWhiteSpace(request.VerificationCode);

            if (!hasChallenge)
            {
                throw new InvalidOperationException(
                    $"CLIENTE_BLOQUEADO: El cliente coincide con un registro de la lista de bloqueo (nombre o Facebook). Motivo: {block.Reason}. Se requiere autorización del SuperAdmin para darlo de alta.");
            }

            var authorized = _verification.Validate(
                request.ChallengeId!, request.VerificationCode!, "customer.block.override", _currentUser.UserId ?? string.Empty);

            if (!authorized)
            {
                throw new InvalidOperationException("El código de verificación es inválido o expiró.");
            }
            // Autorizado por el SuperAdmin: continúa el alta pese al bloqueo.
        }

        var duplicate = await _context.Customers
            .AnyAsync(c => c.Phone == phone, cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Ya existe un cliente registrado con el teléfono {phone}. El teléfono debe ser único.");
        }

        var entity = new BzaCustomer
        {
            Name = request.Name ?? string.Empty,
            FacebookName = facebookName,
            Phone = phone,
            BzaCollectorId = request.BzaCollectorId,
            Status = 1
        };

        _context.Customers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    /// <summary>Deja solo los dígitos del teléfono para usarlo como llave única.</summary>
    private static string NormalizePhone(string? phone)
        => new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
}