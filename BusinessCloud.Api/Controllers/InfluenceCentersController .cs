using BusinessCloud.Application.Commissions.Dtos;
using BusinessCloud.Application.Commissions.Interfaces;
using BusinessCloud.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/commissions/influence-centers")]
public class InfluenceCentersController : ControllerBase
{
    private readonly IInfluenceCenterService _service;

    public InfluenceCentersController(IInfluenceCenterService service)
    {
        _service = service;
    }

    // Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InfluenceCenterCreateRequest req)
    {
        var created = await _service.CreateAsync(req);

        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            new ApiResponse<InfluenceCenterResponse>
            {
                Success = true,
                Data = created
            });
    }

    // List
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var list = await _service.GetAllAsync(includeInactive);

        return Ok(new ApiResponse<List<InfluenceCenterResponse>>
        {
            Success = true,
            Data = list
        });
    }

    // Get by id
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var item = await _service.GetByIdAsync(id);

        if (item == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Centro de influencia no encontrado"
            });
        }

        return Ok(new ApiResponse<InfluenceCenterResponse>
        {
            Success = true,
            Data = item
        });
    }

    // Update
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] InfluenceCenterUpdateRequest req)
    {
        var updated = await _service.UpdateAsync(id, req);

        return Ok(new ApiResponse<InfluenceCenterResponse>
        {
            Success = true,
            Data = updated
        });
    }

    // Deactivate
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate([FromRoute] int id)
    {
        await _service.DeactivateAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Centro de influencia desactivado"
        });
    }

    // Activate
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate([FromRoute] int id)
    {
        await _service.ActivateAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Centro de influencia activado"
        });
    }

    // Set credentials
    [HttpPost("{id:int}/credentials")]
    public async Task<IActionResult> SetCredentials([FromRoute] int id, [FromBody] InfluenceCenterSetCredentialsRequest req)
    {
        await _service.SetCredentialsAsync(id, req);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Credenciales actualizadas"
        });
    }
}
