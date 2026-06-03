using BusinessCloud.Application.Bazares.Commands.CreateBzaDelivery;
using BusinessCloud.Application.Bazares.Commands.DeleteBzaDelivery;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaDelivery;
using BusinessCloud.Application.Bazares.Queries.GetAllBzaDeliveries;
using BusinessCloud.Application.Bazares.Queries.GetBzaDeliveryDetail;
using BusinessCloud.Application.Bazares.Queries.GetDeliveriesByDateRange;
using BusinessCloud.Application.Bazares.Queries.GetDeliveriesByGroup;
using BusinessCloud.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaDeliveriesController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaDeliveriesController(ISender mediator) => _mediator = mediator;

    #region CRUD Entregas

    /// <summary>
    /// Crear una nueva entrega para un grupo.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBzaDeliveryCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Obtener todas las entregas del tenant.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BzaDeliveryListDto>>> GetAll()
    {
        return await _mediator.Send(new GetAllBzaDeliveriesQuery());
    }

    /// <summary>
    /// Obtener detalle de una entrega por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BzaDeliveryDetailDto>> GetById(int id)
    {
        return await _mediator.Send(new GetBzaDeliveryDetailQuery(id));
    }

    /// <summary>
    /// Actualizar una entrega (fecha, status, notas).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateBzaDeliveryCommand command)
    {
        if (id != command.Id) return BadRequest("El ID no coincide.");
        var result = await _mediator.Send(command);
        return result ? NoContent() : NotFound(new { message = "Entrega no encontrada." });
    }

    /// <summary>
    /// Eliminar una entrega (solo si no tiene items asociados).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteBzaDeliveryCommand(id));
        return result.Success ? NoContent() : BadRequest(new { message = result.Message });
    }

    #endregion

    #region Consultas

    /// <summary>
    /// Obtener entregas por grupo de recolectores.
    /// </summary>
    [HttpGet("by-group/{groupId}")]
    public async Task<ActionResult<List<BzaDeliveryByGroupDto>>> GetByGroup(int groupId)
    {
        return await _mediator.Send(new GetDeliveriesByGroupQuery(groupId));
    }

    /// <summary>
    /// Obtener entregas por rango de fechas (opcional: filtrar por grupo).
    /// </summary>
    [HttpGet("by-date-range")]
    public async Task<ActionResult<List<BzaDeliveryByDateDto>>> GetByDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? groupId)
    {
        var query = new GetDeliveriesByDateRangeQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            BzaCollectorGroupId = groupId
        };
        return await _mediator.Send(query);
    }

    #endregion
}
