using BusinessCloud.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessCloud.Infrastructure.Common.Services;

/// <summary>
/// Implementación no-op de <see cref="IBlobStorageService"/> usada cuando Azure Blob Storage
/// no está configurado. Permite que la API arranque y resuelva la dependencia, pero lanza
/// <see cref="InvalidOperationException"/> si se intenta subir archivos.
/// </summary>
public class NoOpBlobStorageService : IBlobStorageService
{
    private readonly ILogger<NoOpBlobStorageService> _logger;

    public NoOpBlobStorageService(ILogger<NoOpBlobStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Intento de subir archivo '{FileName}' al contenedor '{Container}' sin Azure Blob Storage configurado.", fileName, containerName);
        throw new InvalidOperationException("Azure Blob Storage no está configurado. Configure la cadena de conexión 'AzureBlobStorage' para habilitar la subida de archivos.");
    }

    public Task<bool> DeleteAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Intento de eliminar blob '{FileName}' sin Azure Blob Storage configurado.", fileName);
        return Task.FromResult(false);
    }

    public Task<string?> GetUrlAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }
}
