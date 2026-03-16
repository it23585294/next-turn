using FluentValidation;

namespace NextTurn.Application.Queue.Queries.ListQueueStaffAssignments;

public sealed class ListQueueStaffAssignmentsQueryValidator : AbstractValidator<ListQueueStaffAssignmentsQuery>
{
    public ListQueueStaffAssignmentsQueryValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");
    }
}
