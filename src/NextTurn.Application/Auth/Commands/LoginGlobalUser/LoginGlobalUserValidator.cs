using FluentValidation;

namespace NextTurn.Application.Auth.Commands.LoginGlobalUser;

/// <summary>
/// Validates a LoginGlobalUserCommand — same rules as tenant-scoped login.
/// </summary>
public class LoginGlobalUserValidator : AbstractValidator<LoginGlobalUserCommand>
{
    public LoginGlobalUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
