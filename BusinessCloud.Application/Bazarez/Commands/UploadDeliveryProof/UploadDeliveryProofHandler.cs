using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Domain.Bazares.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.UploadDeliveryProof;

public class UploadDeliveryProofHandler(IBazaresDbContext context, IBlobStorageService blobStorage)
    : IRequestHandler<UploadDeliveryProofCommand, UploadDeliveryProofResultDto>
{
    private const string ContainerName = "bazarez";
    private const string DirectoryName = "entregas";
    private readonly IBazaresDbContext _context = context;
    private readonly IBlobStorageService _blobStorage = blobStorage;

    public async Task<UploadDeliveryProofResultDto> Handle(UploadDeliveryProofCommand request, CancellationToken cancellationToken)
    {
        var ev = await _context.ClosureEvents
            .FirstOrDefaultAsync(c => c.Id == request.ClosureEventId, cancellationToken)
            ?? throw new KeyNotFoundException("El evento de cierre no existe.");

        if (!ev.InDeliveryProcess)
            throw new InvalidOperationException("El evento aún no está en proceso de entrega.");

        if (request.Files.Count == 0)
            throw new InvalidOperationException("Debes adjuntar al menos un archivo.");

        var now = DateTime.UtcNow;
        var createdIds = new List<int>();

        foreach (var file in request.Files)
        {
            var extension = GetExtension(file.FileName, file.ContentType);
            var blobName = $"{DirectoryName}/{request.ClosureEventId}-{Guid.NewGuid():N}{extension}";
            var url = await _blobStorage.UploadAsync(
                ContainerName, blobName, file.Content, file.ContentType, cancellationToken);

            var proof = new BzaClosureDeliveryProof
            {
                TenantId = ev.TenantId,
                BzaClosureEventId = ev.Id,
                BzaCollectorGroupId = request.CollectorGroupId,
                ImageUrl = url,
                UploadedAt = now
            };
            _context.ClosureDeliveryProofs.Add(proof);
            await _context.SaveChangesAsync(cancellationToken);
            createdIds.Add(proof.Id);
        }

        return new UploadDeliveryProofResultDto { Success = true, CreatedProofIds = createdIds };
    }

    private static string GetExtension(string fileName, string contentType)
    {
        var ext = System.IO.Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(ext))
            return ext.ToLowerInvariant();

        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => ".jpg"
        };
    }
}