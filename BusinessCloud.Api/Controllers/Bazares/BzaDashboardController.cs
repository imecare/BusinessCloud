using BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;
using BusinessCloud.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaDashboardController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaDashboardController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Dashboard del bazar con filtro de periodo (today, week, month).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<BzaDashboardDto>> Get([FromQuery] string? period = null)
    {
        return await _mediator.Send(new GetBzaDashboardQuery(period));
    }
}

