using Microsoft.AspNetCore.Mvc;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using BusinessCloud.Application.Bazares.Queries.GetBazarSettings;
using BusinessCloud.Application.Bazares.Commands.UpdateBazarSettings;
using BusinessCloud.Application.Bazares.Commands.UploadBazarLogo;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaBazarSettingsController(ISender mediator) : ControllerBase
{
    /// <summary>Obtiene la configuración general del bazar (identidad, contacto y redes).</summary>
    [HttpGet]
    public async Task<ActionResult<BazarSettingsDto>> Get()
        => await mediator.Send(new GetBazarSettingsQuery());

    /// <summary>Actualiza (upsert) la configuración general del bazar.</summary>
    [HttpPut]
    public async Task<ActionResult> Update(UpdateBazarSettingsCommand command)
    {
        await mediator.Send(command);
        return NoContent();
    }

    /// <summary>Sube el logo del bazar y devuelve su URL pública.</summary>
    [HttpPost("logo")]
    [RequestSizeLimit(8_000_000)]
    public async Task<ActionResult<UploadBazarLogoResultDto>> UploadLogo(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Debes adjuntar una imagen.");
        }

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(new UploadBazarLogoCommand(
            stream, file.FileName, file.ContentType));
        return Ok(result);
    }
}
