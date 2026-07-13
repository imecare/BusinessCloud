using BusinessCloud.Application.Bazares.Commands.CreateBzaCustomer;
using BusinessCloud.Application.Bazares.Commands.BlockCustomer;
using BusinessCloud.Application.Bazares.Commands.MergeBzaCustomers;
using BusinessCloud.Application.Bazares.Commands.UpdateBzaCustomer;
using BusinessCloud.Application.Bazares.Commands.CommitBzaCustomersImport;
using BusinessCloud.Application.Bazares.Queries.GetBzaCustomers;
using BusinessCloud.Application.Bazares.Queries.GetBlockedCustomers;
using BusinessCloud.Application.Bazares.Queries.GetBzaCustomersTemplate;
using BusinessCloud.Application.Bazares.Queries.GetMergeCandidates;
using BusinessCloud.Application.Bazares.Queries.SearchBzaCustomers;
using BusinessCloud.Application.Bazares.Queries.ValidateBzaCustomersImport;
using MediatR;
using BusinessCloud.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

[Authorize]
[RequireModule("Bazares")]
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaCustomersController : ControllerBase
{
    private readonly ISender _mediator;
    public BzaCustomersController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<BzaCustomerDto>>> GetAll()
        => await _mediator.Send(new GetBzaCustomersQuery());

    [HttpGet("search")]
    public async Task<ActionResult<List<BzaCustomerSearchDto>>> Search([FromQuery] string? query)
        => await _mediator.Send(new SearchBzaCustomersQuery(query));

    [HttpGet("merge-candidates")]
    public async Task<ActionResult<List<MergeCandidateDto>>> MergeCandidates([FromQuery] int[] ids)
        => await _mediator.Send(new GetMergeCandidatesQuery(ids ?? []));

    [HttpPost("merge")]
    public async Task<ActionResult<MergeBzaCustomersResultDto>> Merge(MergeBzaCustomersCommand command)
        => await _mediator.Send(command);

    [HttpPost]
    public async Task<ActionResult<int>> Create(CreateBzaCustomerCommand command)
        => await _mediator.Send(command);

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, UpdateBzaCustomerCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("El ID del cliente no coincide.");
        }

        await _mediator.Send(command);
        return NoContent();
    }

    #region Lista de bloqueo de clientes

    /// <summary>Lista de clientes bloqueados (vetados).</summary>
    [HttpGet("blocked")]
    public async Task<ActionResult<List<BlockedCustomerDto>>> GetBlocked([FromQuery] bool includeInactive = false)
        => await _mediator.Send(new GetBlockedCustomersQuery(includeInactive));

    /// <summary>Agrega un cliente a la lista de bloqueo (con motivo).</summary>
    [HttpPost("blocked")]
    public async Task<ActionResult<int>> Block(BlockCustomerCommand command)
        => await _mediator.Send(command);

    /// <summary>Quita (desactiva) un bloqueo. Solo SuperAdmin, con verificación por WhatsApp.</summary>
    [Authorize(Policy = "SuperAdmin")]
    [HttpPost("blocked/{id:int}/unblock")]
    public async Task<ActionResult> Unblock(int id, [FromBody] UnblockRequest? body)
    {
        await _mediator.Send(new UnblockCustomerCommand(id, body?.ChallengeId, body?.VerificationCode));
        return NoContent();
    }

    #endregion

    #region Importación Masiva de Clientes

    /// <summary>Descargar plantilla Excel para importar clientes.</summary>
    [HttpGet("import/template")]
    public async Task<IActionResult> DownloadImportTemplate()
    {
        var result = await _mediator.Send(new GetBzaCustomersTemplateQuery());
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>PASO 1: Validar (sin guardar) un archivo Excel de clientes.</summary>
    [HttpPost("import/validate")]
    public async Task<ActionResult<ValidateBzaCustomersImportResult>> ValidateImport(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Archivo vacío o no proporcionado." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var result = await _mediator.Send(new ValidateBzaCustomersImportQuery(ms.ToArray()));
        return Ok(result);
    }

    /// <summary>PASO 2: Confirmar y guardar la importación de clientes validada.</summary>
    [HttpPost("import/commit")]
    public async Task<ActionResult<CommitBzaCustomersImportResult>> CommitImport([FromBody] CommitBzaCustomersImportCommand command)
        => Ok(await _mediator.Send(command));

    #endregion
}

/// <summary>Cuerpo de la petición para quitar un bloqueo (con verificación OTP).</summary>
public class UnblockRequest
{
    public string? ChallengeId { get; set; }
    public string? VerificationCode { get; set; }
}