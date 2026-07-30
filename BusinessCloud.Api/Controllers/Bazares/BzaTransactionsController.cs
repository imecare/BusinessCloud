using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Bazares.Queries.EstimateClosureTransactions;
using BusinessCloud.Application.Bazares.Queries.GetBazaresPackages;
using BusinessCloud.Application.Bazares.Queries.GetBzaTransactionsBalance;
using BusinessCloud.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

/// <summary>
/// Saldo y estimación de transacciones (envío de totales) del bazar.
/// </summary>
[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/transactions")]
public class BzaTransactionsController(ISender mediator) : ControllerBase
{
    /// <summary>Saldo actual de transacciones (disponibles, cortesía, bloqueo).</summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var balance = await mediator.Send(new GetBzaTransactionsBalanceQuery());
        return Ok(new ApiResponse<TransactionsBalanceDto> { Success = true, Data = balance });
    }

    /// <summary>Estima cuántas transacciones consumirá el envío de un cierre antes de confirmar.</summary>
    [HttpGet("estimate/{closureEventId:int}")]
    public async Task<IActionResult> Estimate(int closureEventId, [FromQuery] int[]? customerIds = null)
    {
        var estimate = await mediator.Send(new EstimateClosureTransactionsQuery(
            closureEventId,
            customerIds is { Length: > 0 } ? customerIds : null));
        return Ok(new ApiResponse<ClosureTransactionsEstimateDto> { Success = true, Data = estimate });
    }

    /// <summary>Paquetes extra de transacciones (recargas), ofrecidos al quedar pocas transacciones.</summary>
    [HttpGet("extra-packages")]
    public async Task<IActionResult> GetExtraPackages()
    {
        var packages = await mediator.Send(new GetBazaresPackagesQuery(OnlyExtra: true));
        return Ok(new ApiResponse<IReadOnlyList<PackageDto>> { Success = true, Data = packages });
    }
}