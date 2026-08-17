using BusinessCloud.Application.Bazares.Queries.GetBzaClosureAnalytics;
using BusinessCloud.Application.Bazares.Queries.GetBzaDashboard;
using BusinessCloud.Application.Bazares.Queries.GetBzaSalesChart;
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

    /// <summary>
    /// Datos para las graficas de ventas: por semana en un mes y por mes en un anio.
    /// </summary>
    [HttpGet("sales-chart")]
    public async Task<ActionResult<BzaSalesChartDto>> GetSalesChart([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        return await _mediator.Send(new GetBzaSalesChartQuery(year, month));
    }

    /// <summary>
    /// Metricas de cierres por evento y por mes.
    /// </summary>
    [HttpGet("closure-analytics")]
    public async Task<ActionResult<BzaClosureAnalyticsDto>> GetClosureAnalytics([FromQuery] int? year = null)
    {
        return await _mediator.Send(new GetBzaClosureAnalyticsQuery(year));
    }
}


