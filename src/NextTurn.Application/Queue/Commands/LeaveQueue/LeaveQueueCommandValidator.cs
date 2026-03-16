using FluentValidation;

namespace NextTurn.Application.Queue.Commands.LeaveQueue;

/// <summary>
/// Validates a LeaveQueueCommand before the handler processes it.
///
/// Intentionally minimal — only structural field validation belongs here.
/// Business rules (queue exists, user has active entry) live in the handler
/// because they require async I/O that FluentValidation should not perform.
/// </summary>
public class LeaveQueueCommandValidator : AbstractValidator<LeaveQueueCommand>
{
    public LeaveQueueCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
