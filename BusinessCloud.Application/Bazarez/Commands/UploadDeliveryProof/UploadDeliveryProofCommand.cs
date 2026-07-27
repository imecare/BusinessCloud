using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UploadDeliveryProof;

/// <summary>Archivo individual de comprobante de entrega recibido en la subida.</summary>
public record DeliveryProofFileInput(Stream Content, string FileName, string ContentType);

/// <summary>
/// Sube uno o varios comprobantes de entrega (firma o foto de recibido) para un
/// Evento de Cierre que ya está en proceso de entrega. Si <see cref="CollectorGroupId"/>
/// viene asignado, el comprobante solo aplica a ese grupo; si viene nulo, aplica a
/// todos los clientes del cierre (general).
/// </summary>
public record UploadDeliveryProofCommand(
    int ClosureEventId,
    int? CollectorGroupId,
    IReadOnlyList<DeliveryProofFileInput> Files) : IRequest<UploadDeliveryProofResultDto>;

public class UploadDeliveryProofResultDto
{
    public bool Success { get; set; }
    public List<int> CreatedProofIds { get; set; } = new();
}