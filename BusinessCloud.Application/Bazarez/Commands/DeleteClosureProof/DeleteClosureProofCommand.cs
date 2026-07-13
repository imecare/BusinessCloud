using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeleteClosureProof;

/// <summary>
/// Comando PÚBLICO (sin autenticación) para que el cliente elimine uno de sus
/// comprobantes usando el token, mientras el total esté en espera de aprobación.
/// </summary>
public record DeleteClosureProofCommand(
    string UploadToken,
    int ProofId) : IRequest<DeleteClosureProofResultDto>;

public class DeleteClosureProofResultDto
{
    public bool Success { get; set; }

    /// <summary>Comprobantes que quedan tras eliminar.</summary>
    public int RemainingProofs { get; set; }

    /// <summary>Estado resultante del total (1=Pendiente, 2=ComprobanteRecibido).</summary>
    public int Status { get; set; }
}
