using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Register(RegisterPaymentRequest request)
    {
        var result = await _paymentService.RegisterPaymentAsync(request);
        return Ok(result);
    }
}