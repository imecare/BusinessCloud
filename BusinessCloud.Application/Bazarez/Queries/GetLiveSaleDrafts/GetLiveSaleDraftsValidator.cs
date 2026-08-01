using FluentValidation;

namespace BusinessCloud.Application.Bazares.Queries.GetLiveSaleDrafts;

public class GetLiveSaleDraftsValidator : AbstractValidator<GetLiveSaleDraftsQuery>
{
    public GetLiveSaleDraftsValidator() => RuleFor(x => x.BzaEventId).GreaterThan(0);
}
