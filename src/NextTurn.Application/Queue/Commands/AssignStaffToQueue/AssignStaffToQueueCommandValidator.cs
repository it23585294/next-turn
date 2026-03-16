using FluentValidation;

namespace NextTurn.Application.Queue.Commands.AssignStaffToQueue;

public sealed class AssignStaffToQueueCommandValidator : AbstractValidator<AssignStaffToQueueCommand>
{
    public AssignStaffToQueueCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");

        RuleFor(x => x.StaffUserId)
            .NotEmpty().WithMessage("Staff user ID is required.");
    }
}
