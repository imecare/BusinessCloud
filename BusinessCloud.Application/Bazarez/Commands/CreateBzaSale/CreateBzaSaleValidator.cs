using FluentValidation;

namespace BusinessCloud.Application.Bazares.Commands.CreateBzaSale;

public class CreateBzaSaleValidator : AbstractValidator<CreateBzaSaleCommand>
{
    public CreateBzaSaleValidator()
    {
        RuleFor(v => v.BzaCustomerId).GreaterThan(0);

        RuleFor(v => v.Products)
            .NotEmpty().WithMessage("La venta debe tener al menos un producto.");

        RuleForEach(v => v.Products).ChildRules(p => {
            p.RuleFor(x => x.Description).NotEmpty();
            p.RuleFor(x => x.Price).GreaterThan(0);
        });
    }
}