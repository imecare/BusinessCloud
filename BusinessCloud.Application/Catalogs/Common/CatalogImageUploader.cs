using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Application.Catalogs.Common;

/// <summary>
/// Sube imágenes del catálogo a Azure Blob Storage (contenedor "catalogs") validando
/// tamaño y tipo real del archivo (firma de bytes), sin confiar en el nombre ni el content-type del cliente.
/// </summary>
public static class CatalogImageUploader
{
    public const string ContainerName = "catalogs";
    public const long MaxBytes = 5 * 1024 * 1024;

    public static async Task<(string Url, string BlobName)> UploadAsync(
        IBlobStorageService blob, string tenantId, string kind, Stream content, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
            throw new ArgumentException("Debes adjuntar una imagen.");
        if (buffer.Length > MaxBytes)
            throw new ArgumentException("La imagen no debe pesar más de 5 MB.");

        var bytes = buffer.GetBuffer();
        var (contentType, extension) = Detect(bytes, (int)buffer.Length)
            ?? throw new ArgumentException("Formato de imagen no permitido. Usa JPG, PNG o WebP.");

        buffer.Position = 0;
        var blobName = $"{tenantId}/{kind}/{Guid.NewGuid():N}{extension}";
        var url = await blob.UploadAsync(ContainerName, blobName, buffer, contentType, ct);
        return (url, blobName);
    }

    public static async Task DeleteQuietlyAsync(IBlobStorageService blob, string? blobName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;
        try
        {
            await blob.DeleteAsync(ContainerName, blobName, ct);
        }
        catch
        {
            // Un blob huérfano no debe impedir la operación del usuario.
        }
    }

    private static (string ContentType, string Extension)? Detect(byte[] b, int length)
    {
        if (length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return ("image/jpeg", ".jpg");
        if (length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            return ("image/png", ".png");
        if (length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
            && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
            return ("image/webp", ".webp");
        return null;
    }
}
