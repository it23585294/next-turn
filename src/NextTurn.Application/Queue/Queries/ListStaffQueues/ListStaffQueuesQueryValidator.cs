using FluentValidation;

namespace NextTurn.Application.Queue.Queries.ListStaffQueues;

public sealed class ListStaffQueuesQueryValidator : AbstractValidator<ListStaffQueuesQuery>
{
    public ListStaffQueuesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.");

        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");
    }
}
