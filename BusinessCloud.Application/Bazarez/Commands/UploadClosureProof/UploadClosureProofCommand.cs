using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UploadClosureProof;

/// <summary>Archivo individual de comprobante recibido en la subida.</summary>
public record ClosureProofFileInput(Stream Content, string FileName, string ContentType);

/// <summary>
/// Comando PÚBLICO (sin autenticación) para que el cliente suba uno o varios comprobantes
/// de pago usando el token recibido en el mensaje de totales. Permite adjuntar varios
/// archivos (p. ej. cuando el pago se hizo en depósitos separados).
/// </summary>
public record UploadClosureProofCommand(
    string UploadToken,
    IReadOnlyList<ClosureProofFileInput> Files,
    string? Justification = null,
    int? PaymentMethod = null,
    string? Reference = null,
    string? WithdrawalBank = null) : IRequest<UploadClosureProofResultDto>;

public class UploadClosureProofResultDto
{
    public bool Success { get; set; }

    /// <summary>URL del último comprobante subido (compatibilidad).</summary>
    public string ProofImageUrl { get; set; } = string.Empty;

    /// <summary>URLs de todos los comprobantes vigentes tras la subida.</summary>
    public List<string> ProofImageUrls { get; set; } = new();
}
