using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/public/[controller]")]
[AllowAnonymous] // Requerimiento: Consulta pública sin registro complejo 
public class PublicSalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicSalesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Consulta el historial de pagos y saldos mediante Teléfono.
    /// </summary>
    /// <param name="phone">Teléfono del cliente</param>
    /// <param name="tenantId">ID de la empresa (enviado por la App o Header)</param>
    [HttpGet("history/{phone}")]
    public async Task<IActionResult> GetCustomerHistory(string phone, [FromHeader(Name = "X-Tenant-Id")] string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest("El identificador de empresa (Tenant) es obligatorio.");

        // Ejecutamos la Query que lee de MongoDB y Redis [cite: 79, 86]
        // Nota: El QueryHandler debe estar preparado para recibir el tenantId manualmente 
        // si no hay un token JWT presente.
        var query = new GetCustomerHistoryQuery(phone, null);
        var result = await _mediator.Send(query);

        if (result == null || !result.Any())
            return NotFound("No se encontró historial para los datos proporcionados.");

        return Ok(result);
    }
}