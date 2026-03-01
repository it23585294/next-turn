using FluentValidation;

namespace NextTurn.Application.Auth.Commands.LoginUser;

/// <summary>
/// Validates a LoginUserCommand before the handler processes it.
///
/// Intentionally minimal — login validation does NOT re-check password complexity.
/// We only verify the fields are present and well-formed enough to attempt a lookup.
/// The handler enforces the real security rules (correct password, not locked out).
///
/// No "user not found" validation here — that lives in the handler so that the
/// error message ("Invalid credentials") is identical for both wrong-password and
/// unknown-email cases, preventing user enumeration attacks.
/// </summary>
public class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
