using BusinessCloud.Application.Common.Interfaces;

namespace BusinessCloud.Api.Common;

/// <summary>
/// Almacenamiento de archivos en el sistema de archivos local.
/// Pensado para entornos de desarrollo, cuando no hay acceso a Azure Blob Storage.
/// Guarda los archivos bajo {ContentRoot}/uploads/{contenedor}/{archivo} y los
/// expone como archivos estáticos en la ruta pública "/uploads".
/// La URL devuelta es absoluta (esquema + host de la petición actual), por lo que
/// funciona sin configurar puertos.
/// </summary>
public class LocalFileBlobStorageService : IBlobStorageService
{
    private readonly string _rootPath;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string? _publicBaseUrl;

    public LocalFileBlobStorageService(
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _rootPath = Path.Combine(env.ContentRootPath, "uploads");
        _httpContextAccessor = httpContextAccessor;
        _publicBaseUrl = configuration["BlobStorage:LocalBaseUrl"];
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(
        string containerName,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = Sanitize(containerName);
        var relative = fileName.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(_rootPath, container, relative.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            if (content.CanSeek)
                content.Position = 0;
            await content.CopyToAsync(fs, cancellationToken);
        }

        return $"{GetBaseUrl()}/uploads/{container}/{relative}";
    }

    public Task<bool> DeleteAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, Sanitize(containerName),
            fileName.Replace('\\', '/').TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<string?> GetUrlAsync(string containerName, string fileName, CancellationToken cancellationToken = default)
    {
        var container = Sanitize(containerName);
        var relative = fileName.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(_rootPath, container, relative.Replace('/', Path.DirectorySeparatorChar));

        return File.Exists(fullPath)
            ? Task.FromResult<string?>($"{GetBaseUrl()}/uploads/{container}/{relative}")
            : Task.FromResult<string?>(null);
    }

    private string GetBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_publicBaseUrl))
            return _publicBaseUrl.TrimEnd('/');

        var req = _httpContextAccessor.HttpContext?.Request;
        return req is not null ? $"{req.Scheme}://{req.Host}" : string.Empty;
    }

    private static string Sanitize(string name) => name.Trim('/', '\\');
}
