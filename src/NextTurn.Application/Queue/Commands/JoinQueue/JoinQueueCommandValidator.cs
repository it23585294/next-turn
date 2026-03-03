using FluentValidation;

namespace NextTurn.Application.Queue.Commands.JoinQueue;

/// <summary>
/// Validates a JoinQueueCommand before the handler processes it.
///
/// Intentionally minimal — only structural field validation belongs here.
/// Business rules (queue exists, not full, not already joined) live in the handler
/// because they require async I/O that FluentValidation should not perform.
/// </summary>
public class JoinQueueCommandValidator : AbstractValidator<JoinQueueCommand>
{
    public JoinQueueCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
