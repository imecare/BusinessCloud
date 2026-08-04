using Microsoft.AspNetCore.Mvc;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using BusinessCloud.Application.Bazares.Commands.CreateCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.UpdateCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.DeleteCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.ActivateCollectorGroup;
using BusinessCloud.Application.Bazares.Commands.DeactivateCollectorGroup;
using BusinessCloud.Application.Bazares.Queries.GetCollectorGroups;
using BusinessCloud.Application.Bazares.Queries.GetGlobalCollectorGroups;
using BusinessCloud.Application.Bazares.Commands.ImportGlobalCollectorGroups;
using BusinessCloud.Shared.Responses;

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

    [HttpGet("global-catalog")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GlobalCollectorGroupDto>>>> GetGlobalCatalog()
    {
        var groups = await mediator.Send(new GetGlobalCollectorGroupsQuery());
        return Ok(new ApiResponse<IReadOnlyList<GlobalCollectorGroupDto>>
        {
            Success = true,
            Message = $"Se encontraron {groups.Count} grupos en la Base general.",
            Data = groups,
        });
    }

    [HttpPost("import-global")]
    public async Task<ActionResult<ApiResponse<ImportGlobalCollectorGroupsResult>>> ImportGlobal(
        ImportGlobalCollectorGroupsCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(new ApiResponse<ImportGlobalCollectorGroupsResult>
        {
            Success = true,
            Message = $"Importación completada: {result.GroupsCreated} grupos y {result.CollectorsCreated} recolectores creados; " +
                      $"{result.GroupsReused} grupos reutilizados y {result.CollectorsSkipped} recolectores omitidos por duplicado.",
            Data = result,
        });
    }

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

    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult> Deactivate(int id)
    {
        await mediator.Send(new DeactivateCollectorGroupCommand(id));
        return NoContent();
    }
}
