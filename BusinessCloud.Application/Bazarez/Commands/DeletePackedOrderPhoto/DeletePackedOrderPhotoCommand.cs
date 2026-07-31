using MediatR;

namespace BusinessCloud.Application.Bazares.Commands.DeletePackedOrderPhoto;

public record DeletePackedOrderPhotoCommand(int PhotoId) : IRequest<DeletePackedOrderPhotoResultDto>;

public record DeletePackedOrderPhotoResultDto(bool Success, int RemainingPhotos);