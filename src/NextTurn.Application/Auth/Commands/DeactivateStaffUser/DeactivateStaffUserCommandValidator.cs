using FluentValidation;

namespace NextTurn.Application.Auth.Commands.DeactivateStaffUser;

public sealed class DeactivateStaffUserCommandValidator : AbstractValidator<DeactivateStaffUserCommand>
{
    public DeactivateStaffUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
