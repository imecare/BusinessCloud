using BusinessCloud.Application.Company.Dtos;
using BusinessCloud.Application.Company.Queries.GetCompanyContext;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PayCompanyController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayCompanyController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Obtiene el contexto de la empresa del usuario autenticado.
    /// Incluye el CompanyCode que debe compartir con sus clientes
    /// para que puedan consultar su historial en el endpoint público.
    /// </summary>
    [HttpGet("context")]
    public async Task<ActionResult<CompanyContextDto>> GetContext(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCompanyContextQuery(), cancellationToken);
        return result is null ? NotFound("No se encontró empresa para el usuario autenticado.") : Ok(result);
    }
}