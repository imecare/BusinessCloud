using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeleteDeliveryProof;

/// <summary>Elimina un comprobante de entrega subido por error.</summary>
public record DeleteDeliveryProofCommand(int ProofId) : IRequest<DeleteDeliveryProofResultDto>;

public class DeleteDeliveryProofResultDto
{
    public bool Success { get; set; }
}