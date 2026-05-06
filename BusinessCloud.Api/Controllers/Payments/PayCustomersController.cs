using BusinessCloud.Application.Payments.Commands.CreateCustomer;
using BusinessCloud.Application.Payments.Commands.UpdateCustomer;
using BusinessCloud.Application.Payments.Queries.GetAllCustomers;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerById;
using BusinessCloud.Application.Payments.Queries.GetMyCustomers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PayCustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayCustomersController(IMediator mediator) => _mediator = mediator;

    [Authorize(Policy = "SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (command is null) return BadRequest();
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.Id != id) return BadRequest();
        var result = await _mediator.Send(command, cancellationToken);
        return result ? Ok(new { success = true, message = "Cliente actualizado." })
                      : NotFound(new { success = false, message = "Cliente no encontrado." });
    }

    [Authorize(Policy = "SuperAdmin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Todos los clientes del tenant. Solo SuperAdmin.
    /// </summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllCustomersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Clientes del comisionista autenticado (filtrado por sellerId del token).
    /// Solo Commissionist.
    /// </summary>
    [Authorize(Policy = "Commissionist")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyCustomers(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyCustomersQuery(), cancellationToken);
        return Ok(result);
    }
}