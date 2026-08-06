using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;

public class UpdateBzaCustomerHandler : IRequestHandler<UpdateBzaCustomerCommand>
{
    private readonly IBazaresDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateBzaCustomerHandler(IBazaresDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateBzaCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Customers
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Cliente de bazar con ID {request.Id} no encontrado.");
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
                ?? throw new Exception($"El recolector con ID {collectorId} no existe.");
        }

        var facebookName = FacebookMessengerProfile.Normalize(request.FacebookName);

        if (request.HasNoWhatsApp)
        {
            // Cliente sin nÃºmero de WhatsApp: conserva su placeholder si ya lo tenÃ­a;
            // si venÃ­a con telÃ©fono real (o sin placeholder), se le asigna uno nuevo.
            if (!entity.HasNoWhatsApp || !NoWhatsAppNumber.IsPlaceholder(entity.Phone))
            {
                var tenantId = _currentUser.TenantId ?? string.Empty;
                entity.Phone = await NoWhatsAppNumber.ReserveNextAsync(_context, tenantId, cancellationToken);
            }
            entity.HasNoWhatsApp = true;
        }
        else
        {
            // TelÃ©fono real: es la llave para el envÃ­o de totales, Ãºnico entre clientes.
            var phone = PhoneNumberNormalizer.Normalize(request.Phone);

            var duplicate = await _context.Customers
                .AnyAsync(c => c.Phone == phone && c.Id != request.Id, cancellationToken);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    $"Ya existe otro cliente registrado con el telÃ©fono {phone}. El telÃ©fono debe ser Ãºnico.");
            }

            entity.Phone = phone;
            entity.HasNoWhatsApp = false;
        }

        entity.Name = request.Name;
        entity.FacebookName = facebookName;
        entity.Status = request.Status;
        entity.BzaCollectorId = collector.Id;
        entity.Collector = collector;
        // Al editar/completar el cliente ya se cuenta con recolector real:
        // deja de estar "pendiente de completar informaciÃ³n" (alta rÃ¡pida).
        entity.IsPendingInfo = false;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
