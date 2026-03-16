using FluentValidation;

namespace NextTurn.Application.Auth.Commands.InviteStaffUser;

public sealed class InviteStaffUserCommandValidator : AbstractValidator<InviteStaffUserCommand>
{
    public InviteStaffUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(254).WithMessage("Email must not exceed 254 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .When(x => x.Phone is not null);
    }
}
