using BusinessCloud.Application.Admin.Commands.PayCommissions;
using BusinessCloud.Application.Admin.Commands.UpsertSystemSeller;
using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Admin.Queries.GetSellerCommissions;
using BusinessCloud.Application.Admin.Queries.GetSystemSellers;
using BusinessCloud.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Admin;

/// <summary>
/// Panel de administración: comisionistas del SaaS, sus comisiones y pagos.
/// Requiere el rol global PlatformAdmin.
/// </summary>
[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/sellers")]
public class AdminSellersController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminSellersController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista los comisionistas con sus totales de comisiones.</summary>
    [HttpGet]
    public async Task<IActionResult> GetSellers([FromQuery] bool? onlyActive)
    {
        var sellers = await _mediator.Send(new GetSystemSellersQuery(onlyActive));
        return Ok(new ApiResponse<IReadOnlyList<SystemSellerDto>> { Success = true, Data = sellers });
    }

    /// <summary>Crea un comisionista.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateSeller([FromBody] SystemSellerRequest request)
    {
        var id = await _mediator.Send(new UpsertSystemSellerCommand
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = request.IsActive,
            DefaultInitialAmount = request.DefaultInitialAmount,
            DefaultMonthlyPercent = request.DefaultMonthlyPercent,
        });

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Comisionista creado.",
            Data = new { id }
        });
    }

    /// <summary>Actualiza un comisionista.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSeller(int id, [FromBody] SystemSellerRequest request)
    {
        await _mediator.Send(new UpsertSystemSellerCommand
        {
            Id = id,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = request.IsActive,
            DefaultInitialAmount = request.DefaultInitialAmount,
            DefaultMonthlyPercent = request.DefaultMonthlyPercent,
        });

        return Ok(new ApiResponse<object> { Success = true, Message = "Comisionista actualizado." });
    }

    /// <summary>Lista las comisiones de un comisionista.</summary>
    [HttpGet("{id:int}/commissions")]
    public async Task<IActionResult> GetCommissions(int id, [FromQuery] bool onlyUnpaid = false)
    {
        var commissions = await _mediator.Send(new GetSellerCommissionsQuery(id, onlyUnpaid));
        return Ok(new ApiResponse<IReadOnlyList<SellerCommissionDto>> { Success = true, Data = commissions });
    }

    /// <summary>Marca comisiones como pagadas (todas las pendientes si no se indican Ids).</summary>
    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> PayCommissions(int id, [FromBody] PayCommissionsRequest request)
    {
        var result = await _mediator.Send(new PayCommissionsCommand
        {
            SystemSellerId = id,
            CommissionIds = request.CommissionIds,
            Note = request.Note,
        });

        return Ok(new ApiResponse<PayCommissionsResult>
        {
            Success = true,
            Message = $"Comisiones pagadas: {result.PaidCount} · Total: {result.TotalPaid:0.00}",
            Data = result
        });
    }
}
