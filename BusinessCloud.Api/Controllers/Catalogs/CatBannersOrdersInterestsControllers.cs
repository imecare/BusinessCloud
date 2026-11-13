using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Catalogs.Commands.Banners;
using BusinessCloud.Application.Catalogs.Commands.Orders;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Catalogs.Queries.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Catalogs;

public record BannerUpdateRequest(string? Title, int? LinkCategoryId, int SortOrder, bool IsActive);
public record OrderStatusRequest(string Status);
public record InterestAttendedRequest(bool IsAttended);

[Authorize]
[RequireModule("Catalogs")]
[ApiController]
[Route("api/catalogs/admin/banners")]
public class CatBannersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BannerDto>>> List()
        => Ok(await mediator.Send(new GetBannersQuery()));

    [HttpPost]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<BannerDto>> Create(
        [FromForm] IFormFile file, [FromForm] string? title, [FromForm] int? linkCategoryId)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Debes adjuntar una imagen." });
        await using var stream = file.OpenReadStream();
        return await mediator.Send(new CreateBannerCommand(stream, title, linkCategoryId));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BannerDto>> Update(int id, BannerUpdateRequest request)
        => await mediator.Send(new UpdateBannerCommand(id, request.Title, request.LinkCategoryId, request.SortOrder, request.IsActive));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteBannerCommand(id));
        return NoContent();
    }
}

[Authorize]
[RequireModule("Catalogs")]
[ApiController]
[Route("api/catalogs/admin/orders")]
public class CatOrdersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListDto>>> List(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => await mediator.Send(new GetOrdersQuery(status, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id)
        => await mediator.Send(new GetOrderQuery(id));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OrderDto>> Adjust(int id, OrderAdjustInput input)
        => await mediator.Send(new AdjustOrderCommand(id, input));

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> SetStatus(int id, OrderStatusRequest request)
        => await mediator.Send(new SetOrderStatusCommand(id, request.Status));
}

[Authorize]
[RequireModule("Catalogs")]
[ApiController]
[Route("api/catalogs/admin/interests")]
public class CatInterestsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<InterestDto>>> List(
        [FromQuery] bool? attended, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => await mediator.Send(new GetInterestsQuery(attended, page, pageSize));

    [HttpPatch("{id:int}/attended")]
    public async Task<IActionResult> SetAttended(int id, InterestAttendedRequest request)
    {
        await mediator.Send(new SetInterestAttendedCommand(id, request.IsAttended));
        return NoContent();
    }
}
