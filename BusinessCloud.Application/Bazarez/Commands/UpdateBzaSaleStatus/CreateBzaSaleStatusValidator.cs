using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.UpdateBzaSaleStatus;

public class CreateBzaSaleStatusValidator : AbstractValidator<UpdateBzaSaleStatusCommand>
{
    public CreateBzaSaleStatusValidator()
    {
        RuleFor(v => v.SaleId).NotEmpty();
        RuleFor(v => v.NewStatus).InclusiveBetween(1, 5); // Según tus códigos de status
    }
}