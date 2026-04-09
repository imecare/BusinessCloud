using BusinessCloud.Application.Payments.Commands.CreateCustomer;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCustomerById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Sellers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SellersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SellersController(IMediator mediator) => _mediator = mediator;

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
}