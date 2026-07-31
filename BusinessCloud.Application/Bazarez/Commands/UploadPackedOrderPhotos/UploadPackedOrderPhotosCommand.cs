using BusinessCloud.Application.Bazares.Common;
using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.UploadPackedOrderPhotos;

public record PackedOrderPhotoFileInput(Stream Content, string FileName, string ContentType);

public record UploadPackedOrderPhotosCommand(
    int ClosureCustomerTotalId,
    IReadOnlyList<PackedOrderPhotoFileInput> Files) : IRequest<UploadPackedOrderPhotosResultDto>;

public class UploadPackedOrderPhotosResultDto
{
    public bool Success { get; set; }
    public List<PackedOrderPhotoDto> Photos { get; set; } = [];
}