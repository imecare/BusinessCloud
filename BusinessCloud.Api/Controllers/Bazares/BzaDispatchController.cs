using BusinessCloud.Application.Bazares.Commands.CreateDispatchSheet;
using BusinessCloud.Application.Bazares.Commands.CreateDispatchSheet;
using BusinessCloud.Application.Bazares.Commands.SignDispatchSheet;
using BusinessCloud.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaDispatchController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaDispatchController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Generar hoja de despacho para un recolector con las ventas listas para entrega.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DispatchSheetResultDto>> Create(CreateDispatchSheetCommand command)
    {
        return await _mediator.Send(command);
    }

    /// <summary>
    /// Firma digital del recolector al recibir los paquetes.
    /// </summary>
    [HttpPatch("{id}/sign")]
    public async Task<ActionResult> Sign(int id, [FromBody] SignDispatchRequest request)
    {
        await _mediator.Send(new SignDispatchSheetCommand(id, request.SignatureBase64));
        return Ok(new { success = true, message = "Hoja de despacho firmada." });
    }
}

public class SignDispatchRequest
{
    public string SignatureBase64 { get; set; } = string.Empty;
}
