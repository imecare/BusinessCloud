using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.StartClosureDelivery;

/// <summary>
/// Marca un evento de cierre como "en proceso de entrega" (tras imprimir etiquetas
/// y/o la hoja de despacho). No modifica el estado de pago.
/// </summary>
public record StartClosureDeliveryCommand(int ClosureEventId) : IRequest<StartClosureDeliveryResultDto>;

public class StartClosureDeliveryResultDto
{
    public int ClosureEventId { get; set; }
    public bool InDeliveryProcess { get; set; }
}

public class StartClosureDeliveryHandler(IBazaresDbContext context)
    : IRequestHandler<StartClosureDeliveryCommand, StartClosureDeliveryResultDto>
{
    public async Task<StartClosureDeliveryResultDto> Handle(StartClosureDeliveryCommand request, CancellationToken ct)
    {
        var closure = await context.ClosureEvents
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, ct)
            ?? throw new KeyNotFoundException("El evento de entrega no existe.");

        if (!closure.InDeliveryProcess)
        {
            closure.InDeliveryProcess = true;
            await context.SaveChangesAsync(ct);
        }

        return new StartClosureDeliveryResultDto
        {
            ClosureEventId = closure.Id,
            InDeliveryProcess = closure.InDeliveryProcess
        };
    }
}
