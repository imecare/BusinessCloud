using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.QuickCreateBzaCustomer;

/// <summary>
/// Crea un cliente con solo el nombre (sin teléfono ni recolector), útil cuando se está
/// capturando una venta en vivo y solo se tiene el nombre a la mano. El cliente se asigna
/// temporalmente a un recolector placeholder ("Sin asignar") y queda marcado con
/// <see cref="BzaCustomer.IsPendingInfo"/> = true hasta que se complete su información.
/// </summary>
public class QuickCreateBzaCustomerHandler : IRequestHandler<QuickCreateBzaCustomerCommand, int>
{
    private const string PlaceholderCollectorName = "Sin asignar";

    private readonly IBazaresDbContext _context;

    public QuickCreateBzaCustomerHandler(IBazaresDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(QuickCreateBzaCustomerCommand request, CancellationToken cancellationToken)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var nameLower = name.ToLower();

        var isBlocked = await _context.BlockedCustomers
            .AsNoTracking()
            .AnyAsync(b => b.IsActive && b.Name.ToLower() == nameLower, cancellationToken);

        if (isBlocked)
        {
            throw new InvalidOperationException(
                $"CLIENTE_BLOQUEADO: El nombre \"{name}\" coincide con un registro de la lista de bloqueo. " +
                "Da de alta al cliente desde el formulario completo para autorizar el alta.");
        }

        var phone = request.Phone?.Trim() ?? string.Empty;
        if (phone.Length > 0 && await _context.Customers.AsNoTracking().AnyAsync(c => c.Phone == phone, cancellationToken))
            throw new InvalidOperationException("Ya existe un cliente con ese numero de WhatsApp.");

        var collector = request.BzaCollectorId is not null
            ? await _context.Collectors.FirstOrDefaultAsync(c => c.Id == request.BzaCollectorId, cancellationToken)
                ?? throw new KeyNotFoundException("Recolector no encontrado.")
            : await _context.Collectors.FirstOrDefaultAsync(
                c => c.Name.ToLower() == PlaceholderCollectorName.ToLower(), cancellationToken);

        var entity = new BzaCustomer
        {
            Name = name,
            FacebookName = string.IsNullOrWhiteSpace(request.FacebookName) ? null : request.FacebookName.Trim(),
            Phone = phone,
            Status = 1,
            IsPendingInfo = string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(request.FacebookName),
        };

        if (collector is null)
        {
            collector = new BzaCollector { Name = PlaceholderCollectorName, IsActive = true };
            _context.Collectors.Add(collector);
        }

        entity.Collector = collector;

        _context.Customers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
