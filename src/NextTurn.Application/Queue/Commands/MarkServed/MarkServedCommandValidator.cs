using FluentValidation;

namespace NextTurn.Application.Queue.Commands.MarkServed;

public sealed class MarkServedCommandValidator : AbstractValidator<MarkServedCommand>
{
    public MarkServedCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");
    }
}
