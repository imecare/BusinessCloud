using Microsoft.AspNetCore.Mvc;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using BusinessCloud.Application.Bazares.Commands.CreateCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.UpdateCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.DeleteCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.ActivateCollectorGroup;
using BusinessCloud.Application.Bazares.Queries.GetCollectorGroups;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaCollectorGroupsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CollectorGroupDto>>> GetAll([FromQuery] bool includeInactive = false)
        => await mediator.Send(new GetCollectorGroupsQuery(includeInactive));

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateCollectorGroupCommand command)
        => await mediator.Send(command);

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateCollectorGroupCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID del grupo no coincide.");
        }

        await mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteCollectorGroupCommand(id));
        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    public async Task<ActionResult> Activate(int id)
    {
        await mediator.Send(new ActivateCollectorGroupCommand(id));
        return NoContent();
    }
}
