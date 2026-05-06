using BusinessCloud.Application.Payments.Commands.CreateSale;
using BusinessCloud.Application.Payments.Commands.CreateSale;
using BusinessCloud.Application.Payments.Commands.MarkCommissionPaid;
using BusinessCloud.Application.Payments.Commands.UpdateSale;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetAllSales;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using BusinessCloud.Application.Payments.Queries.GetMySales;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PaySalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaySalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateSaleCommand command, CancellationToken cancellationToken)
    {
        if (command is null) return BadRequest();
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.Id != id) return BadRequest();
        var result = await _mediator.Send(command, cancellationToken);
        return result ? Ok(new { success = true, message = "Venta actualizada." })
                      : NotFound(new { success = false, message = "Venta no encontrada." });
    }

    /// <summary>
    /// Todas las ventas del tenant. Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllSalesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Historial de un cliente por phone/rfc. Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<CustomerHistoryDto>>> GetHistory([FromQuery] string? phone, [FromQuery] string? rfc, CancellationToken cancellationToken)
    {
        var query = new GetCustomerHistoryQuery(phone ?? string.Empty, rfc);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Ventas del comisionista autenticado (filtrado por sellerId del token).
    /// </summary>
    [Authorize(Policy = "Commissionist")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMySales(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMySalesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Marcar comisión como pagada/revertida. Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPatch("{id:int}/commission-paid")]
    public async Task<IActionResult> MarkCommissionPaid(int id, [FromBody] MarkCommissionPaidRequest request, CancellationToken cancellationToken)
    {
        var command = new MarkCommissionPaidCommand(id, request.Paid, request.Note);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }
}

public class MarkCommissionPaidRequest
{
    public required bool Paid { get; set; }
    public string? Note { get; set; }
}