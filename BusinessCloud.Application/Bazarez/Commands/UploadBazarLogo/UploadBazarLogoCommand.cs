using MediatR;
using Microsoft.EntityFrameworkCore;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;

namespace BusinessCloud.Application.Bazares.Commands.UploadBazarLogo;

/// <summary>
/// Sube el logo del bazar a Blob Storage (carpeta logos dentro del contenedor bazarez)
/// y guarda la URL en la configuración del bazar.
/// </summary>
public record UploadBazarLogoCommand(
    Stream FileContent,
    string FileName,
    string ContentType) : IRequest<UploadBazarLogoResultDto>;

public class UploadBazarLogoResultDto
{
    public bool Success { get; set; }
    public string LogoUrl { get; set; } = string.Empty;
}

public class UploadBazarLogoHandler(IBazaresDbContext context, IBlobStorageService blobStorage)
    : IRequestHandler<UploadBazarLogoCommand, UploadBazarLogoResultDto>
{
    private const string ContainerName = "bazarez";
    private const string DirectoryName = "logos";

    public async Task<UploadBazarLogoResultDto> Handle(UploadBazarLogoCommand request, CancellationToken ct)
    {
        var settings = await context.BazarSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new BzaBazarSettings();
            context.BazarSettings.Add(settings);
        }

        var extension = GetExtension(request.FileName, request.ContentType);
        var blobName = $"{DirectoryName}/{Guid.NewGuid():N}{extension}";
        var url = await blobStorage.UploadAsync(
            ContainerName, blobName, request.FileContent, request.ContentType, ct);

        settings.LogoUrl = url;
        await context.SaveChangesAsync(ct);

        return new UploadBazarLogoResultDto { Success = true, LogoUrl = url };
    }

    private static string GetExtension(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(ext))
        {
            return ext.ToLowerInvariant();
        }

        return contentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/svg+xml" => ".svg",
            _ => ".bin",
        };
    }
}
