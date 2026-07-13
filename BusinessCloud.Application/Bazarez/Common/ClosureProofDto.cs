namespace BusinessCloud.Application.Bazares.Common;

/// <summary>Comprobante de pago subido por el cliente (Id + URL + fecha).</summary>
public record ClosureProofDto(int Id, string Url, DateTime UploadedAt);
