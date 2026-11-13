using BusinessCloud.Application.Catalogs.Commands.Public;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Catalogs.Queries.Public;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BusinessCloud.Api.Controllers.Catalogs;

/// <summary>
/// Catálogo público de una empresa, identificado por su slug (link /c/{slug}). Sin autenticación.
/// Si la empresa no existe, está inactiva, sin el módulo o con suscripción suspendida, responde 404.
/// </summary>
[AllowAnonymous]
[ApiController]
[EnableRateLimiting("catalogs-public")]
[Route("api/catalogs/public/{slug}")]
public class CatPublicController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PublicStoreDto>> GetStore(string slug)
        => await mediator.Send(new GetPublicStoreQuery(slug));

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<PublicCategoryDto>>> GetCategories(string slug)
        => Ok(await mediator.Send(new GetPublicCategoriesQuery(slug)));

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<PublicProductListDto>>> GetProducts(
        string slug, [FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] bool? featured,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 24)
        => await mediator.Send(new GetPublicProductsQuery(slug, search, categoryId, featured, page, pageSize));

    [HttpGet("featured")]
    public async Task<ActionResult<IReadOnlyList<PublicProductListDto>>> GetFeatured(string slug)
        => Ok(await mediator.Send(new GetPublicFeaturedQuery(slug)));

    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<PublicProductDto>> GetProduct(string slug, int id)
        => await mediator.Send(new GetPublicProductQuery(slug, id));

    [HttpPost("orders")]
    [EnableRateLimiting("catalogs-orders")]
    public async Task<ActionResult<PublicOrderResultDto>> CreateOrder(string slug, PublicOrderInput input)
        => await mediator.Send(new CreatePublicOrderCommand(slug, input));

    [HttpPost("interest")]
    [EnableRateLimiting("catalogs-orders")]
    public async Task<IActionResult> CreateInterest(string slug, PublicInterestInput input)
    {
        await mediator.Send(new CreatePublicInterestCommand(slug, input));
        return NoContent();
    }
}
