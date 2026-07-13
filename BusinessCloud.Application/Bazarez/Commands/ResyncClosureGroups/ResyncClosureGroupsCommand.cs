using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.ResyncClosureGroups;

/// <summary>
/// Re-sincroniza el grupo de recolección guardado en cada total de un cierre
/// (<see cref="BzaClosureCustomerTotal.BzaCollectorGroupId"/>) con el grupo actual
/// del recolector asignado al cliente. Se usa cuando se reasignó el recolector de
/// uno o varios clientes después de haber enviado los totales, para que la logística
/// de entrega los muestre en el grupo correcto.
/// </summary>
public record ResyncClosureGroupsCommand(int ClosureEventId) : IRequest<ResyncClosureGroupsResultDto>;

public class ResyncClosureGroupsResultDto
{
    public int ClosureEventId { get; set; }
    public int TotalCount { get; set; }
    public int UpdatedCount { get; set; }
}

public class ResyncClosureGroupsHandler(IBazaresDbContext context)
    : IRequestHandler<ResyncClosureGroupsCommand, ResyncClosureGroupsResultDto>
{
    public async Task<ResyncClosureGroupsResultDto> Handle(ResyncClosureGroupsCommand request, CancellationToken ct)
    {
        var closure = await context.ClosureEvents
            .Include(c => c.CustomerTotals)
                .ThenInclude(t => t.Customer)
                    .ThenInclude(cu => cu.Collector)
            .Include(c => c.GroupDeliveries)
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, ct)
            ?? throw new KeyNotFoundException("El evento de entrega no existe.");

        var updated = 0;

        foreach (var total in closure.CustomerTotals)
        {
            var currentGroupId = total.Customer?.Collector?.BzaCollectorGroupId;
            if (currentGroupId == total.BzaCollectorGroupId)
                continue;

            total.BzaCollectorGroupId = currentGroupId;
            updated++;

            // Si el nuevo grupo no tiene fecha de entrega en este cierre, se crea una
            // usando la fecha oficial (o el límite de pago como respaldo) para que el
            // cliente reasignado siga teniendo una fecha coherente.
            if (currentGroupId.HasValue &&
                closure.GroupDeliveries.All(g => g.BzaCollectorGroupId != currentGroupId.Value))
            {
                closure.GroupDeliveries.Add(new BzaClosureGroupDelivery
                {
                    BzaCollectorGroupId = currentGroupId.Value,
                    DeliveryDate = closure.OfficialDeliveryDate ?? closure.PaymentDeadline
                });
            }
        }

        if (updated > 0)
            await context.SaveChangesAsync(ct);

        return new ResyncClosureGroupsResultDto
        {
            ClosureEventId = closure.Id,
            TotalCount = closure.CustomerTotals.Count,
            UpdatedCount = updated
        };
    }
}
