using BusinessCloud.Application.Payments.Commands.CreateCustomer;
using BusinessCloud.Application.Payments.Queries.GetAllCustomers;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Customers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        if (command is null) return BadRequest();
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet] // Ruta: GET /api/customers
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllCustomersQuery());
        return Ok(result);
    }
}