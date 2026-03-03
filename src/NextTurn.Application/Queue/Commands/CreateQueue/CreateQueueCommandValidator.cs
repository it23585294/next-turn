using FluentValidation;

namespace NextTurn.Application.Queue.Commands.CreateQueue;

/// <summary>
/// Validates a <see cref="CreateQueueCommand"/> before the handler processes it.
///
/// Structural field validation only — business rules (org exists, etc.) live in the handler
/// because they require async I/O that FluentValidation should not perform.
/// </summary>
public class CreateQueueCommandValidator : AbstractValidator<CreateQueueCommand>
{
    public CreateQueueCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Queue name is required.")
            .MaximumLength(200).WithMessage("Queue name must not exceed 200 characters.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThanOrEqualTo(1).WithMessage("Queue capacity must be at least 1.");

        RuleFor(x => x.AverageServiceTimeSeconds)
            .GreaterThanOrEqualTo(1).WithMessage("Average service time must be at least 1 second.");
    }
}
