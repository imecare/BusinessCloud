using BusinessCloud.Application.Bazares.Commands.UploadClosureProof;
using BusinessCloud.Application.Bazares.Commands.DeleteClosureProof;
using BusinessCloud.Application.Bazares.Queries.GetClosureCustomerByToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BusinessCloud.Api.Controllers.Bazares;

/// <summary>
/// Portal público de comprobantes (sin autenticación). Acceso por token de subida
/// recibido en el mensaje de totales.
/// </summary>
[ApiController]
[Route("api/bazares/[controller]")]
public class BzaComprobantesController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Obtiene el total a pagar y datos de entrega del cliente por su token.
    /// </summary>
    [HttpGet("{token}")]
    public async Task<ActionResult<ClosureCustomerPublicDto>> GetByToken(string token)
        => await mediator.Send(new GetClosureCustomerByTokenQuery(token));

    /// <summary>
    /// Sube uno o varios comprobantes de pago del cliente.
    /// </summary>
    [HttpPost("{token}/upload")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<UploadClosureProofResultDto>> Upload(
        string token,
        [FromForm] List<IFormFile> files,
        [FromForm] string? justification = null,
        [FromForm] int? paymentMethod = null,
        [FromForm] string? reference = null)
    {
        // Compatibilidad: aceptar también el campo antiguo "file" (un solo archivo).
        var incoming = (files ?? new List<IFormFile>())
            .Concat(Request.Form.Files.Where(f => f.Name == "file"))
            .Where(f => f is not null && f.Length > 0)
            .DistinctBy(f => (f.Name, f.FileName, f.Length))
            .ToList();

        if (incoming.Count == 0)
        {
            return BadRequest("Debes adjuntar al menos un archivo.");
        }

        var streams = new List<Stream>();
        try
        {
            var inputs = new List<ClosureProofFileInput>();
            foreach (var f in incoming)
            {
                var stream = f.OpenReadStream();
                streams.Add(stream);
                inputs.Add(new ClosureProofFileInput(stream, f.FileName, f.ContentType));
            }

            var result = await mediator.Send(new UploadClosureProofCommand(token, inputs, justification, paymentMethod, reference));
            return Ok(result);
        }
        finally
        {
            foreach (var s in streams)
            {
                await s.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Elimina uno de los comprobantes del cliente (solo mientras esté en espera de aprobación).
    /// </summary>
    [HttpDelete("{token}/proofs/{proofId:int}")]
    public async Task<ActionResult<DeleteClosureProofResultDto>> DeleteProof(string token, int proofId)
    {
        var result = await mediator.Send(new DeleteClosureProofCommand(token, proofId));
        return Ok(result);
    }
}
