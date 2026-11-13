using BusinessCloud.Api.Authorization;
using BusinessCloud.Application.Catalogs.Commands.Categories;
using BusinessCloud.Application.Catalogs.Dtos;
using BusinessCloud.Application.Catalogs.Queries.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Catalogs;

public record CategoryOrderRequest(List<OrderItemSortDto> Items);

[Authorize]
[RequireModule("Catalogs")]
[ApiController]
[Route("api/catalogs/admin/categories")]
public class CatCategoriesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List()
        => Ok(await mediator.Send(new GetCategoriesQuery()));

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CategoryInput input)
        => await mediator.Send(new CreateCategoryCommand(input));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryInput input)
        => await mediator.Send(new UpdateCategoryCommand(id, input));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }

    [HttpPut("order")]
    public async Task<IActionResult> Reorder(CategoryOrderRequest request)
    {
        await mediator.Send(new ReorderCategoriesCommand(request.Items ?? []));
        return NoContent();
    }

    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<UploadResultDto>> UploadImage(int id, IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Debes adjuntar una imagen." });
        await using var stream = file.OpenReadStream();
        return await mediator.Send(new UploadCategoryImageCommand(id, stream));
    }
}
