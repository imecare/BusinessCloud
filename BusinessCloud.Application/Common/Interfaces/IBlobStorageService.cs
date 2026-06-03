namespace BusinessCloud.Application.Common.Interfaces;

public interface IBlobStorageService
{
    /// <summary>
    /// Sube un archivo a Azure Blob Storage.
    /// </summary>
    /// <param name="containerName">Nombre del contenedor.</param>
    /// <param name="fileName">Nombre del archivo.</param>
    /// <param name="content">Contenido del archivo.</param>
    /// <param name="contentType">Tipo MIME del archivo.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>URL pública del archivo subido.</returns>
    Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un archivo de Azure Blob Storage.
    /// </summary>
    Task<bool> DeleteAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la URL de un archivo existente.
    /// </summary>
    Task<string?> GetUrlAsync(string containerName, string fileName, CancellationToken cancellationToken = default);
}
