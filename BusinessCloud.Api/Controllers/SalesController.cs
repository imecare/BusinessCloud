using BusinessCloud.Application.Commissions.Interfaces;
using BusinessCloud.Application.Payments.Dtos;
using BusinessCloud.Application.Payments.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Payments;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create(CreateSaleRequest request)
    {
        var result = await _saleService.CreateSaleAsync(request);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<SaleResponse>>> GetHistory(string rfc, string phone)
    {
        var result = await _saleService.GetCustomerHistoryAsync(rfc, phone);
        return Ok(result);
    }
}