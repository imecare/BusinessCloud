using BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;
using BusinessCloud.Application.Bazares.Queries.GetBzaCustomers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[ApiController]
[Route("api/bazares/[controller]")]
public class BzaCustomersController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaCustomersController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<BzaCustomerDto>>> GetAll()
        => await _mediator.Send(new GetBzaCustomersQuery());

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBzaCustomerCommand command)
        => await _mediator.Send(command);

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateBzaCustomerCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID del cliente no coincide.");
        }

        await _mediator.Send(command);
        return NoContent();
    }
}