using BusinessCloud.Application.Payments.Commands.CreateSale;
using BusinessCloud.Application.Payments.Queries.GetCustomerHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    // Inyectamos únicamente IMediator
    public SalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateSaleCommand command)
    {
        // El controlador ya no conoce la lógica, solo envía el comando
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<CustomerHistoryDto>>> GetHistory([FromQuery] string phone, [FromQuery] string? rfc)
    {
        // Disparamos la consulta optimizada de MongoDB/Redis
        var query = new GetCustomerHistoryQuery(phone, rfc);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}