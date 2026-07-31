using BusinessCloud.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Bazares.Commands.DeletePackedOrderPhoto;

public class DeletePackedOrderPhotoHandler(IBazaresDbContext context, IBlobStorageService blobStorage)
    : IRequestHandler<DeletePackedOrderPhotoCommand, DeletePackedOrderPhotoResultDto>
{
    private const string ContainerName = "bazarez";

    public async Task<DeletePackedOrderPhotoResultDto> Handle(
        DeletePackedOrderPhotoCommand request,
        CancellationToken cancellationToken)
    {
        var photo = await context.PackedOrderPhotos
            .FirstOrDefaultAsync(p => p.Id == request.PhotoId, cancellationToken)
            ?? throw new KeyNotFoundException("La foto del pedido empacado no existe.");

        var totalId = photo.BzaClosureCustomerTotalId;
        context.PackedOrderPhotos.Remove(photo);
        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(photo.BlobName))
            await blobStorage.DeleteAsync(ContainerName, photo.BlobName, cancellationToken);

        var remaining = await context.PackedOrderPhotos
            .CountAsync(p => p.BzaClosureCustomerTotalId == totalId, cancellationToken);

        return new DeletePackedOrderPhotoResultDto(true, remaining);
    }
}