using BusinessCloud.Application.Bazares.Commands.UploadClosureProof;
using BusinessCloud.Application.Bazares.Commands.DeleteClosureProof;
using BusinessCloud.Application.Bazares.Commands.Notifications;
using BusinessCloud.Application.Bazares.Queries.GetClosureCustomerByToken;
using BusinessCloud.Application.Bazares.Common;
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
    /// Construye URLs completas para el logo y otras imágenes usando la URL del servidor actual.
    /// </summary>
    [HttpGet("{token}")]
    public async Task<ActionResult<ClosureCustomerPublicDto>> GetByToken(string token)
    {
        var result = await mediator.Send(new GetClosureCustomerByTokenQuery(token));

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // Construir URL completa del logo si es relativa
        if (!string.IsNullOrEmpty(result.BazarLogoUrl) && !result.BazarLogoUrl.StartsWith("http"))
        {
            result.BazarLogoUrl = result.BazarLogoUrl.StartsWith("/")
                ? $"{baseUrl}{result.BazarLogoUrl}"
                : $"{baseUrl}/{result.BazarLogoUrl}";
        }

        // Construir URLs completas para imágenes de comprobantes (ClosureProofDto es un record, crear nuevas instancias)
        if (result.Proofs != null && result.Proofs.Count > 0)
        {
            result.Proofs = result.Proofs
                .Select(proof =>
                {
                    var url = proof.Url;
                    if (!string.IsNullOrEmpty(url) && !url.StartsWith("http"))
                    {
                        url = url.StartsWith("/") ? $"{baseUrl}{url}" : $"{baseUrl}/{url}";
                    }
                    return new ClosureProofDto(proof.Id, url, proof.UploadedAt);
                })
                .ToList();
        }

        // Construir URLs para otros bazares pendientes
        if (result.OtherPendingAccounts != null && result.OtherPendingAccounts.Count > 0)
        {
            result.OtherPendingAccounts = result.OtherPendingAccounts
                .Select(account =>
                {
                    var logoUrl = account.BazarLogoUrl;
                    if (!string.IsNullOrEmpty(logoUrl) && !logoUrl.StartsWith("http"))
                    {
                        logoUrl = logoUrl.StartsWith("/") ? $"{baseUrl}{logoUrl}" : $"{baseUrl}/{logoUrl}";
                    }
                    return new OtherPendingAccountDto(account.BazarName, logoUrl, account.UploadToken);
                })
                .ToList();
        }

        return result;
    }

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
        [FromForm] string? reference = null,
        [FromForm] string? withdrawalBank = null)
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

            var result = await mediator.Send(new UploadClosureProofCommand(token, inputs, justification, paymentMethod, reference, withdrawalBank));
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

    /// <summary>
    /// Registra o actualiza la suscripción Web Push del navegador del cliente.
    /// </summary>
    [HttpPost("{token}/subscribe")]
    public async Task<ActionResult<SubscribeToWebPushResultDto>> Subscribe(string token, [FromBody] SubscribeWebPushRequest body)
    {
        var result = await mediator.Send(new SubscribeToWebPushCommand(
            token,
            body.Endpoint ?? string.Empty,
            body.Keys?.P256dh ?? string.Empty,
            body.Keys?.Auth ?? string.Empty));

        return Ok(result);
    }
}

public class SubscribeWebPushRequest
{
    public string? Endpoint { get; set; }
    public SubscribeWebPushKeys? Keys { get; set; }
}

public class SubscribeWebPushKeys
{
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
}
