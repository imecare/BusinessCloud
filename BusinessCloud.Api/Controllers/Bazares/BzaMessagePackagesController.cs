using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Bazares.Commands.CreateMessagePackageRequest;
using BusinessCloud.Application.Bazares.Queries.GetBazaresPackages;
using BusinessCloud.Shared.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

/// <summary>
/// Paquetes de mensajes que un bazar puede consultar y solicitar contratar.
/// </summary>
[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/message-packages")]
public class BzaMessagePackagesController(ISender mediator) : ControllerBase
{
    /// <summary>Lista los paquetes de mensajes disponibles (catálogo del admin).</summary>
    [HttpGet]
    public async Task<IActionResult> GetPackages()
    {
        var packages = await mediator.Send(new GetBazaresPackagesQuery());
        return Ok(new ApiResponse<object> { Success = true, Data = packages });
    }

    /// <summary>Solicita contratar un paquete de mensajes (queda pendiente y avisa al super admin).</summary>
    [HttpPost("request")]
    public async Task<IActionResult> RequestPackage([FromBody] RequestPackageBody body)
    {
        var result = await mediator.Send(new CreateMessagePackageRequestCommand(body.PackageId, body.Note));
        return Ok(new ApiResponse<MessagePackageRequestResult>
        {
            Success = true,
            Message = "Solicitud enviada. Te contactaremos para completar la contratación.",
            Data = result
        });
    }

    public class RequestPackageBody
    {
        public int PackageId { get; set; }
        public string? Note { get; set; }
    }
}
