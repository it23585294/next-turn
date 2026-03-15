using FluentValidation;

namespace NextTurn.Application.Queue.Commands.MarkNoShow;

public sealed class MarkNoShowCommandValidator : AbstractValidator<MarkNoShowCommand>
{
    public MarkNoShowCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");
    }
}
