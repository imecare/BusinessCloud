using BusinessCloud.Application.Bazares.Queries.GetCustomerPortal;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[ApiController]
[Route("api/bazares/[controller]")]
public class BzaPortalController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaPortalController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Portal público de auto-gestión del cliente.
    /// Acceso por token único (sin autenticación).
    /// </summary>
    [HttpGet("{token}")]
    public async Task<ActionResult<CustomerPortalDto>> GetByToken(string token)
    {
        var result = await _mediator.Send(new GetCustomerPortalQuery(token));
        return Ok(result);
    }
}
