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

        var placeholderCollector = await _context.Collectors
            .FirstOrDefaultAsync(c => c.Name.ToLower() == PlaceholderCollectorName.ToLower(), cancellationToken);

        var entity = new BzaCustomer
        {
            Name = name,
            Phone = string.Empty,
            Status = 1,
            IsPendingInfo = true,
        };

        if (placeholderCollector is null)
        {
            placeholderCollector = new BzaCollector { Name = PlaceholderCollectorName, IsActive = true };
            _context.Collectors.Add(placeholderCollector);
        }

        entity.Collector = placeholderCollector;

        _context.Customers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
