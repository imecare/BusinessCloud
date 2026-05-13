using BusinessCloud.Application.Payments.Commands.DeletePayment;
using BusinessCloud.Application.Payments.Commands.RegisterPayment;
using BusinessCloud.Application.Payments.Commands.UpdatePayment;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetAllPayments;
using BusinessCloud.Application.Payments.Queries.GetPaymentsBySale;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PayPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayPaymentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<PaymentReceiptDto>> Register(RegisterPaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result); // El Front-end recibe el JSON con saldo y movimientos
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllPaymentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("sale/{saleId:int}")]
    public async Task<IActionResult> GetBySale(int saleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentsBySaleQuery(saleId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentCommand command, CancellationToken cancellationToken)
    {
        if (command is null) return BadRequest();
        var cmd = command with { Id = id };
        var result = await _mediator.Send(cmd, cancellationToken);
        return result ? Ok(new { success = true, message = "Abono actualizado." })
                      : NotFound(new { success = false, message = "Abono no encontrado." });
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeletePaymentCommand(id, reason), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}