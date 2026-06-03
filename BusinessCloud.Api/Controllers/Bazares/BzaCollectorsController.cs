using Microsoft.AspNetCore.Mvc;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using BusinessCloud.Application.Bazares.Commands.CreateCollector;
using BusinessCloud.Application.Bazares.Commands.UpdateCollector;
using BusinessCloud.Application.Bazares.Commands.DeleteCollector;
using BusinessCloud.Application.Bazares.Commands.ActivateCollector;
using BusinessCloud.Application.Bazares.Queries.GetCollectors;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaCollectorsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CollectorDto>>> GetAll([FromQuery] bool includeInactive = false)
        => await mediator.Send(new GetCollectorsQuery(includeInactive));

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateCollectorCommand command)
        => await mediator.Send(command);

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateCollectorCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID del recolector no coincide.");
        }

        await mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteCollectorCommand(id));
        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    public async Task<ActionResult> Activate(int id)
    {
        await mediator.Send(new ActivateCollectorCommand(id));
        return NoContent();
    }
}