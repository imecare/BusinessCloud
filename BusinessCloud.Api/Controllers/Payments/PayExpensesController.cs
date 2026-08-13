using BusinessCloud.Application.Payments.Commands.CreateExpense;
using BusinessCloud.Application.Payments.Commands.DeleteExpense;
using BusinessCloud.Application.Payments.Commands.MarkExpenseReceived;
using BusinessCloud.Application.Payments.Commands.UpdateExpense;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetAllExpenses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PayExpensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayExpensesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Todos los gastos/compras del tenant. Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<List<ExpenseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllExpensesQuery(), cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateExpenseCommand command, CancellationToken cancellationToken)
    {
        if (command is null) return BadRequest();
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id }, id);
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.Id != id) return BadRequest();
        var result = await _mediator.Send(command, cancellationToken);
        return result ? Ok(new { success = true, message = "Gasto actualizado." })
                      : NotFound(new { success = false, message = "Gasto no encontrado." });
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPatch("{id:int}/received")]
    public async Task<IActionResult> MarkReceived(int id, [FromBody] MarkExpenseReceivedRequest request, CancellationToken cancellationToken)
    {
        var received = request?.Received ?? true;
        var result = await _mediator.Send(new MarkExpenseReceivedCommand(id, received), cancellationToken);
        return result
            ? Ok(new { success = true, message = received ? "Compra marcada como recibida." : "Compra marcada como pendiente." })
            : NotFound(new { success = false, message = "Compra no encontrada." });
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteExpenseCommand(id), cancellationToken);
        return result ? Ok(new { success = true, message = "Gasto eliminado." })
                      : NotFound(new { success = false, message = "Gasto no encontrado." });
    }
}

public class MarkExpenseReceivedRequest
{
    public bool Received { get; set; } = true;
}
