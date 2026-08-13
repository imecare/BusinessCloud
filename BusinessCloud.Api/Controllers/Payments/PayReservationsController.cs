using BusinessCloud.Application.Payments.Commands.ConcretizeReservation;
using BusinessCloud.Application.Payments.Commands.CreateReservation;
using BusinessCloud.Application.Payments.Commands.DeleteReservation;
using BusinessCloud.Application.Payments.Commands.UpdateReservation;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetAllReservations;
using BusinessCloud.Application.Payments.Queries.GetMyReservations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PayReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayReservationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Todos los apartados del tenant. Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<List<ReservationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllReservationsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Apartados del comisionista autenticado (filtrado por sellerId del token).
    /// Solo Commissionist.
    /// </summary>
    [Authorize(Policy = "Commissionist")]
    [HttpGet("mine")]
    public async Task<ActionResult<List<ReservationDto>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyReservationsQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateReservationCommand command, CancellationToken cancellationToken)
    {
        if (command is null) return BadRequest();
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservationCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.Id != id) return BadRequest();
        var result = await _mediator.Send(command, cancellationToken);
        return result
            ? Ok(new { success = true, message = "Apartado actualizado." })
            : NotFound(new { success = false, message = "Apartado no encontrado." });
    }

    /// <summary>
    /// Concreta el apartado: lo convierte en venta y lo elimina de apartados.
    /// Devuelve el Id de la nueva venta.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("{id:int}/concretize")]
    public async Task<IActionResult> Concretize(int id, CancellationToken cancellationToken)
    {
        var saleId = await _mediator.Send(new ConcretizeReservationCommand(id), cancellationToken);
        return saleId is null
            ? NotFound(new { success = false, message = "Apartado no encontrado." })
            : Ok(new { success = true, saleId, message = "Apartado concretado como venta." });
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteReservationCommand(id), cancellationToken);
        return result
            ? Ok(new { success = true, message = "Apartado eliminado." })
            : NotFound(new { success = false, message = "Apartado no encontrado." });
    }
}
