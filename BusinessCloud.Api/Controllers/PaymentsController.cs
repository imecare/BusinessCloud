using BusinessCloud.Application.Payments.Commands.RegisterPayment;
using BusinessCloud.Application.Payments.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentReceiptDto>> Register(RegisterPaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result); // El Front-end recibe el JSON con saldo y movimientos
    }
}