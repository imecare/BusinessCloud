using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Catalogs.Commands.Settings;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Catalogs.Queries.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Catalogs;

[Authorize]
[RequireModule("Catalogs")]
[ApiController]
[Route("api/catalogs/admin")]
public class CatSettingsController(ISender mediator) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<ActionResult<StoreSettingsDto>> GetSettings()
        => await mediator.Send(new GetStoreSettingsQuery());

    [HttpPut("settings")]
    public async Task<ActionResult<StoreSettingsDto>> UpdateSettings(StoreSettingsInput input)
        => await mediator.Send(new UpdateStoreSettingsCommand(input));

    [HttpPost("settings/logo")]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<UploadResultDto>> UploadLogo(IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Debes adjuntar una imagen." });
        await using var stream = file.OpenReadStream();
        return await mediator.Send(new UploadLogoCommand(stream));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SummaryDto>> GetSummary()
        => await mediator.Send(new GetCatalogSummaryQuery());
}
