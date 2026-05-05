using BusinessCloud.Application.Payments.Commands.CreateSeller;
using BusinessCloud.Application.Payments.Commands.UpdateSellerStatus;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetActiveSellers;
using BusinessCloud.Application.Payments.Queries.GetAllSellers;
using BusinessCloud.Application.Payments.Queries.GetSellerById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PaySellersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaySellersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSellerCommand command, CancellationToken cancellationToken)
    {
        if (command is null) return BadRequest();
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SellerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetSellerByIdQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Todos los vendedores (activos e inactivos).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllSellersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Solo vendedores activos (StatusId = 1).
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveSellersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activar/desactivar vendedor (borrado lógico).
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSellerStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSellerStatusCommand(id, request.StatusId);
        var result = await _mediator.Send(command, cancellationToken);

        return result
            ? Ok(new { success = true, message = request.StatusId == 1 ? "Vendedor activado." : "Vendedor desactivado." })
            : NotFound(new { success = false, message = "Vendedor no encontrado." });
    }
}

public class UpdateSellerStatusRequest
{
    public required int StatusId { get; set; }
}