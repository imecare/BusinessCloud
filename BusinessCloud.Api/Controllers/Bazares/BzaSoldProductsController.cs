using BusinessCloud.Application.Bazares.Commands.CreateBzaSoldProduct;
using BusinessCloud.Application.Bazares.Commands.DeleteBzaSoldProduct;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaSoldProduct;
using BusinessCloud.Application.Bazares.Queries.GetSoldProductsBySale;
using BusinessCloud.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

/// <summary>
/// Controlador para gestionar Productos Vendidos a clientes dentro de Eventos de Venta.
/// Cada registro representa un producto vendido a UN cliente en UN Evento de Venta específico.
/// NO es un catálogo de productos, es un historial de ventas.
/// </summary>
[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/sold-products")]
public class BzaSoldProductsController(ISender mediator) : ControllerBase
{
    private readonly ISender _mediator = mediator;

    /// <summary>
    /// Registrar un producto vendido a un cliente en un Evento de Venta específico.
    /// </summary>
    /// <returns>ID del registro de producto vendido creado.</returns>
    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBzaSoldProductCommand command)
    {
        var soldProductId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetBySale), new { saleId = command.BzaSaleId }, soldProductId);
    }

    /// <summary>
    /// Obtener todos los productos vendidos de un Evento de Venta específico.
    /// Opcionalmente filtra por customerId.
    /// </summary>
    [HttpGet("by-sale/{saleId}")]
    public async Task<ActionResult<SoldProductsBySaleDto>> GetBySale(int saleId, [FromQuery] int? customerId)
    {
        var query = new GetSoldProductsBySaleQuery(saleId, customerId);
        return await _mediator.Send(query);
    }

    /// <summary>
    /// Modificar datos de un producto vendido (Descripción, precio).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateBzaSoldProductCommand command)
    {
        if (id != command.Id) return BadRequest("El ID no coincide.");
        var result = await _mediator.Send(command);
        return result ? NoContent() : NotFound(new { message = "Producto vendido no encontrado." });
    }

    /// <summary>
    /// Eliminar un registro de producto vendido.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteBzaSoldProductCommand(id));
        return result ? NoContent() : NotFound(new { message = "Producto vendido no encontrado." });
    }
}
