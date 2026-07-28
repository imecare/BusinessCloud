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
    private readonly IAdminPinService _adminPin;

    public CreateBzaCustomerHandler(
        IBazaresDbContext context,
        IVerificationCodeService verification,
        ICurrentUserService currentUser,
        IAdminPinService adminPin)
    {
        _context = context;
        _verification = verification;
        _currentUser = currentUser;
        _adminPin = adminPin;
    }

    public async Task<int> Handle(CreateBzaCustomerCommand request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.Phone);
        var facebookName = FacebookMessengerProfile.Normalize(request.FacebookName);

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
            var userId = _currentUser.UserId ?? string.Empty;
            bool authorized = false;

            // Verificar por PIN si se proporcionó.
            if (!string.IsNullOrWhiteSpace(request.AdminPin))
            {
                authorized = await _adminPin.VerifyPinAsync(userId, request.AdminPin, cancellationToken);
                if (!authorized)
                    throw new InvalidOperationException("PIN incorrecto. No se puede forzar el alta del cliente bloqueado.");
            }
            // Verificar por OTP si se proporcionó challenge.
            else if (!string.IsNullOrWhiteSpace(request.ChallengeId) && !string.IsNullOrWhiteSpace(request.VerificationCode))
            {
                authorized = _verification.Validate(
                    request.ChallengeId!, request.VerificationCode!, "customer.block.override", userId);
                if (!authorized)
                    throw new InvalidOperationException("El código de verificación es inválido o expiró.");
            }

            if (!authorized)
            {
                throw new InvalidOperationException(
                    $"CLIENTE_BLOQUEADO: El cliente coincide con un registro de la lista de bloqueo (nombre o Facebook). Motivo: {block.Reason}. Se requiere autorización del SuperAdmin para darlo de alta.");
            }
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

    private static string NormalizePhone(string? phone)
        => new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
}