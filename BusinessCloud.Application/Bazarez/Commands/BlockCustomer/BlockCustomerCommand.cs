using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.BlockCustomer;

/// <summary>
/// Agrega un cliente a la lista de bloqueo (por nombre y/o Facebook, con un motivo).
/// </summary>
public record BlockCustomerCommand(
    string Name,
    string? FacebookName,
    string? Phone,
    string Reason,
    int? BzaCustomerId) : IRequest<int>;

public class BlockCustomerHandler(IBazaresDbContext context)
    : IRequestHandler<BlockCustomerCommand, int>
{
    public async Task<int> Handle(BlockCustomerCommand request, CancellationToken ct)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var reason = (request.Reason ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("El nombre del cliente a bloquear es obligatorio.");
        if (string.IsNullOrEmpty(reason))
            throw new InvalidOperationException("El motivo del bloqueo es obligatorio.");

        var facebook = string.IsNullOrWhiteSpace(request.FacebookName) ? null : request.FacebookName.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone)
            ? null
            : new string(request.Phone.Where(char.IsDigit).ToArray());

        var entity = new BzaBlockedCustomer
        {
            Name = name,
            FacebookName = facebook,
            Phone = phone,
            Reason = reason,
            BzaCustomerId = request.BzaCustomerId,
            IsActive = true
        };

        context.BlockedCustomers.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity.Id;
    }
}

/// <summary>Desactiva (quita) un bloqueo de la lista. Requiere autorización OTP del SuperAdmin.</summary>
public record UnblockCustomerCommand(int Id, string? ChallengeId = null, string? VerificationCode = null) : IRequest;

public class UnblockCustomerHandler(
    IBazaresDbContext context,
    IVerificationCodeService verification,
    ICurrentUserService currentUser)
    : IRequestHandler<UnblockCustomerCommand>
{
    public async Task Handle(UnblockCustomerCommand request, CancellationToken ct)
    {
        var entity = await context.BlockedCustomers
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("El bloqueo no existe.");

        if (!entity.IsActive)
            return;

        var hasChallenge = !string.IsNullOrWhiteSpace(request.ChallengeId)
                           && !string.IsNullOrWhiteSpace(request.VerificationCode);
        if (!hasChallenge)
            throw new InvalidOperationException("VERIFICATION_REQUIRED: Se requiere autorización del SuperAdmin para quitar el bloqueo.");

        var authorized = verification.Validate(
            request.ChallengeId!, request.VerificationCode!, "customer.unblock", currentUser.UserId ?? string.Empty);
        if (!authorized)
            throw new InvalidOperationException("El código de verificación es inválido o expiró.");

        entity.IsActive = false;
        await context.SaveChangesAsync(ct);
    }
}
