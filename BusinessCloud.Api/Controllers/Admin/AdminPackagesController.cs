using BusinessCloud.Application.Admin.Commands.UpsertPackage;
using BusinessCloud.Application.Admin.Dtos;
using BusinessCloud.Application.Admin.Queries.GetPackages;
using BusinessCloud.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Admin;

/// <summary>
/// Panel de administración: catálogo de paquetes por sistema.
/// Requiere el rol global PlatformAdmin.
/// </summary>
[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/packages")]
public class AdminPackagesController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminPackagesController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Lista los paquetes del catálogo.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPackages([FromQuery] string? module, [FromQuery] bool? onlyActive)
    {
        var packages = await _mediator.Send(new GetPackagesQuery(module, onlyActive));
        return Ok(new ApiResponse<IReadOnlyList<PackageDto>> { Success = true, Data = packages });
    }

    /// <summary>Crea un paquete.</summary>
    [HttpPost]
    public async Task<IActionResult> CreatePackage([FromBody] PackageRequest request)
    {
        var id = await _mediator.Send(new UpsertPackageCommand
        {
            Name = request.Name,
            Module = request.Module,
            Price = request.Price,
            Currency = request.Currency,
            IncludedMessages = request.IncludedMessages,
            IsActive = request.IsActive,
            Description = request.Description,
        });

        return Ok(new ApiResponse<object> { Success = true, Message = "Paquete creado.", Data = new { id } });
    }

    /// <summary>Actualiza un paquete.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePackage(int id, [FromBody] PackageRequest request)
    {
        await _mediator.Send(new UpsertPackageCommand
        {
            Id = id,
            Name = request.Name,
            Module = request.Module,
            Price = request.Price,
            Currency = request.Currency,
            IncludedMessages = request.IncludedMessages,
            IsActive = request.IsActive,
            Description = request.Description,
        });

        return Ok(new ApiResponse<object> { Success = true, Message = "Paquete actualizado." });
    }
}
