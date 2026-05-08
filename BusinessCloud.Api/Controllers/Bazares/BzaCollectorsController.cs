using Microsoft.AspNetCore.Mvc;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using BusinessCloud.Application.Bazares.Commands.CreateCollector;
using BusinessCloud.Application.Bazares.Commands.UpdateCollector;
using BusinessCloud.Application.Bazares.Queries.GetCollectors;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaCollectorsController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaCollectorsController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<CollectorDto>>> GetAll()
        => await _mediator.Send(new GetCollectorsQuery());

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateCollectorCommand command)
        => await _mediator.Send(command);

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateCollectorCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID del recolector no coincide.");
        }

        await _mediator.Send(command);
        return NoContent(); // 204: Éxito sin contenido de retorno
    }
}