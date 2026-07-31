using BusinessCloud.Application.Bazares.Common;
using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UploadPackedOrderPhotos;

public class UploadPackedOrderPhotosHandler(IBazaresDbContext context, IBlobStorageService blobStorage)
    : IRequestHandler<UploadPackedOrderPhotosCommand, UploadPackedOrderPhotosResultDto>
{
    private const string ContainerName = "bazarez";
    private const string DirectoryName = "pedidos-empacados";

    public async Task<UploadPackedOrderPhotosResultDto> Handle(
        UploadPackedOrderPhotosCommand request,
        CancellationToken cancellationToken)
    {
        var total = await context.ClosureCustomerTotals
            .Include(t => t.PackedOrderPhotos)
            .FirstOrDefaultAsync(t => t.Id == request.ClosureCustomerTotalId, cancellationToken)
            ?? throw new KeyNotFoundException("El cliente no existe en este cierre.");

        var now = DateTime.UtcNow;
        foreach (var file in request.Files)
        {
            var extension = GetExtension(file.FileName, file.ContentType);
            var blobName = $"{DirectoryName}/{total.Id}-{Guid.NewGuid():N}{extension}";
            var url = await blobStorage.UploadAsync(
                ContainerName,
                blobName,
                file.Content,
                file.ContentType,
                cancellationToken);

            total.PackedOrderPhotos.Add(new BzaPackedOrderPhoto
            {
                TenantId = total.TenantId,
                ImageUrl = url,
                BlobName = blobName,
                UploadedAt = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return new UploadPackedOrderPhotosResultDto
        {
            Success = true,
            Photos = total.PackedOrderPhotos
                .OrderBy(p => p.UploadedAt)
                .ThenBy(p => p.Id)
                .Select(p => new PackedOrderPhotoDto(p.Id, p.ImageUrl, p.UploadedAt))
                .ToList()
        };
    }

    private static string GetExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
            return extension.ToLowerInvariant();

        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
    }
}