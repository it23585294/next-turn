using FluentValidation;

namespace NextTurn.Application.Queue.Commands.UnassignStaffFromQueue;

public sealed class UnassignStaffFromQueueCommandValidator : AbstractValidator<UnassignStaffFromQueueCommand>
{
    public UnassignStaffFromQueueCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");

        RuleFor(x => x.StaffUserId)
            .NotEmpty().WithMessage("Staff user ID is required.");
    }
}
