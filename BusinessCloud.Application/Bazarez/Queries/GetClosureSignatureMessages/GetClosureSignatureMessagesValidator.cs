using FluentValidation;

namespace BusinessCloud.Application.Bazares.Queries.GetClosureSignatureMessages;

public class GetClosureSignatureMessagesValidator : AbstractValidator<GetClosureSignatureMessagesQuery>
{
    public GetClosureSignatureMessagesValidator()
    {
        RuleFor(query => query.ClosureEventId).GreaterThan(0);
    }
}
