using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.DeletePackedOrderPhoto;

public class DeletePackedOrderPhotoValidator : AbstractValidator<DeletePackedOrderPhotoCommand>
{
    public DeletePackedOrderPhotoValidator()
    {
        RuleFor(x => x.PhotoId)
            .GreaterThan(0).WithMessage("La foto es requerida.");
    }
}