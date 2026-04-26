using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetPublicHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BusinessCloud.Api.Controllers.Payments;

[AllowAnonymous]
[ApiController]
[Route("payment/[controller]")]
public class PayPublicSalesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PayPublicSalesController> _logger;

    public PayPublicSalesController(IMediator mediator, ILogger<PayPublicSalesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Consulta pública de historial de ventas y abonos.
    /// No requiere autenticación JWT ni header X-Tenant-Id.
    /// El companyCode se resuelve internamente a TenantId.
    /// </summary>
    /// <response code="200">Cliente encontrado (con o sin movimientos)</response>
    /// <response code="400">Datos de request inválidos</response>
    /// <response code="404">Cliente no encontrado para la combinación indicada</response>
    [HttpPost("history/query")]
    [EnableRateLimiting("public-history")]
    [ProducesResponseType(typeof(PublicHistoryApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PublicHistoryApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHistory(
        [FromBody] PublicHistoryLookupRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Intento de consulta pública - CompanyCode: {CompanyCode}, Phone: {Phone}",
            request.CompanyCode,
            request.Phone);

        var query = new GetPublicHistoryQuery(request.Phone, request.Rfc, request.CompanyCode);
        var result = await _mediator.Send(query, cancellationToken);

        // Escenario 1: Cliente NO existe
        if (!result.CustomerFound)
        {
            _logger.LogWarning(
                "Cliente no encontrado - CompanyCode: {CompanyCode}, Phone: {Phone}",
                request.CompanyCode,
                request.Phone);

            return NotFound(new PublicHistoryApiResponse
            {
                StatusCode = "CUSTOMER_NOT_FOUND",
                Message = "No existe un cliente con ese RFC y teléfono para la empresa indicada.",
                Data = null
            });
        }

        // Escenario 2: Cliente existe SIN movimientos
        if (!result.Data!.HasMovements)
        {
            return Ok(new PublicHistoryApiResponse
            {
                StatusCode = "CUSTOMER_WITHOUT_MOVEMENTS",
                Message = "El cliente existe, pero no tiene movimientos registrados.",
                Data = result.Data
            });
        }

        // Escenario 3: Cliente existe CON movimientos
        return Ok(new PublicHistoryApiResponse
        {
            StatusCode = "OK",
            Message = "Consulta exitosa.",
            Data = result.Data
        });
    }
}