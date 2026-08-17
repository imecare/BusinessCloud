using FluentValidation;

namespace BusinessCloud.Application.Bazares.Queries.GetBzaClosureAnalytics;

public class GetBzaClosureAnalyticsValidator : AbstractValidator<GetBzaClosureAnalyticsQuery>
{
    public GetBzaClosureAnalyticsValidator()
    {
        RuleFor(query => query.Year)
            .InclusiveBetween(1, 9999)
            .When(query => query.Year.HasValue);
    }
}
