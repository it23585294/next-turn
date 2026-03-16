using FluentValidation;

namespace NextTurn.Application.Auth.Commands.ReactivateStaffUser;

public sealed class ReactivateStaffUserCommandValidator : AbstractValidator<ReactivateStaffUserCommand>
{
    public ReactivateStaffUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
