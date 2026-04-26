using BusinessCloud.Application.Bazares.Commands.CreateBzaSale;
using BusinessCloud.Application.Bazares.Queries.GetBzaSaleDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaSaleStatus;

namespace BusinessCloud.Api.Controllers.Bazares;

[ApiController]
[Route("api/bazares/[controller]")]
public class BzaSalesController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaSalesController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBzaSaleCommand command)
    {
        return await _mediator.Send(command);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BzaSaleDetailDto>> GetById(int id)
    {
        return await _mediator.Send(new GetBzaSaleDetailQuery(id));
    }

    [HttpPatch("status")]
    public async Task<ActionResult> UpdateStatus(UpdateBzaSaleStatusCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}