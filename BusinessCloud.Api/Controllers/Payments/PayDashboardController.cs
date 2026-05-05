using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Queries.GetCommissionistStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("payment/[controller]")]
public class PayDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayDashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Estadísticas del comisionista autenticado.
    /// Solo Commissionist.
    /// </summary>
    [Authorize(Policy = "Commissionist")]
    [HttpGet("commissionist-stats")]
    public async Task<ActionResult<CommissionistStatsDto>> GetCommissionistStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCommissionistStatsQuery(), cancellationToken);
        return Ok(result);
    }
}