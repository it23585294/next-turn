using FluentValidation;

namespace NextTurn.Application.Queue.Commands.CallNext;

public sealed class CallNextCommandValidator : AbstractValidator<CallNextCommand>
{
    public CallNextCommandValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");
    }
}
