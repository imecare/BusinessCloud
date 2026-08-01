using FluentValidation;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureWhatsAppStatus;

public class GetClosureWhatsAppStatusValidator : AbstractValidator<GetClosureWhatsAppStatusQuery>
{
    public GetClosureWhatsAppStatusValidator()
        => RuleFor(x => x.ClosureEventId).GreaterThan(0);
}
