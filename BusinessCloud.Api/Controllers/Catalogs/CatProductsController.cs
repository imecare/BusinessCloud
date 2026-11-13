using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Catalogs.Commands.Products;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Catalogs.Queries.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Catalogs;

public record ProductStatusRequest(string Status);
public record ProductFeaturedRequest(bool IsFeatured);
public record VariantStockRequest(int Stock);
public record ImageOrderRequest(List<int> ImageIds);

[Authorize]
[RequireModule("Catalogs")]
[ApiController]
[Route("api/catalogs/admin/products")]
public class CatProductsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListDto>>> List(
        [FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] string? status,
        [FromQuery] bool? featured, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => await mediator.Send(new GetProductsQuery(search, categoryId, status, featured, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id)
        => await mediator.Send(new GetProductQuery(id));

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductInput input)
        => await mediator.Send(new CreateProductCommand(input));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductInput input)
        => await mediator.Send(new UpdateProductCommand(id, input));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, ProductStatusRequest request)
    {
        await mediator.Send(new SetProductStatusCommand(id, request.Status));
        return NoContent();
    }

    [HttpPatch("{id:int}/featured")]
    public async Task<IActionResult> SetFeatured(int id, ProductFeaturedRequest request)
    {
        await mediator.Send(new SetProductFeaturedCommand(id, request.IsFeatured));
        return NoContent();
    }

    [HttpPut("{id:int}/variants/{variantId:int}/stock")]
    public async Task<IActionResult> SetStock(int id, int variantId, VariantStockRequest request)
    {
        await mediator.Send(new SetVariantStockCommand(id, variantId, request.Stock));
        return NoContent();
    }

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<ProductImageDto>> UploadImage(int id, IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Debes adjuntar una imagen." });
        await using var stream = file.OpenReadStream();
        return await mediator.Send(new UploadProductImageCommand(id, stream));
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        await mediator.Send(new DeleteProductImageCommand(id, imageId));
        return NoContent();
    }

    [HttpPut("{id:int}/images/order")]
    public async Task<IActionResult> ReorderImages(int id, ImageOrderRequest request)
    {
        await mediator.Send(new ReorderProductImagesCommand(id, request.ImageIds ?? []));
        return NoContent();
    }
}
