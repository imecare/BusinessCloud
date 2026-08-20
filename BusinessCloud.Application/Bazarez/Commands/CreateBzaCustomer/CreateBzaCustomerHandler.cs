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

        string phone;
        if (request.HasNoWhatsApp)
        {
            // Cliente sin n�mero de WhatsApp: se le asigna un placeholder consecutivo
            // por bazar (10 d�gitos) para respetar la restricción de tel�fono �nico.
            var tenantId = _currentUser.TenantId ?? string.Empty;
            phone = await NoWhatsAppNumber.ReserveNextAsync(_context, tenantId, cancellationToken);
        }
        else
        {
            phone = PhoneNumberNormalizer.Normalize(request.Phone);

            var duplicate = await _context.Customers
                .AnyAsync(c => c.Phone == phone, cancellationToken);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    $"Ya existe un cliente registrado con el tel�fono {phone}. El tel�fono debe ser �nico.");
            }
        }

        BzaCollector collector;
        if (request.HasNoCollector)
        {
            collector = await NoCollectorCustomer.GetOrCreateAsync(_context, cancellationToken);
        }
        else
        {
            var collectorId = request.BzaCollectorId ?? 0;
            collector = await _context.Collectors
                .FirstOrDefaultAsync(c => c.Id == collectorId, cancellationToken)
                ?? throw new InvalidOperationException($"El recolector con ID {collectorId} no existe.");
        }

        var entity = new BzaCustomer
        {
            Name = request.Name ?? string.Empty,
            FacebookName = facebookName,
            Phone = phone,
            HasNoWhatsApp = request.HasNoWhatsApp,
            BzaCollectorId = collector.Id,
            Collector = collector,
            Status = 1
        };        _context.Customers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

