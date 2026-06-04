using FluentValidation;

namespace BusinessCloud.Application.Payments.Commands.UpdateSeller;

public class UpdateSellerValidator : AbstractValidator<UpdateSellerCommand>
{
    public UpdateSellerValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
    }
}
