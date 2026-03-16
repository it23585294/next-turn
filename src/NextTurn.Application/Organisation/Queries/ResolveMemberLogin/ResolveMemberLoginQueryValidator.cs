using FluentValidation;

namespace NextTurn.Application.Organisation.Queries.ResolveMemberLogin;

public sealed class ResolveMemberLoginQueryValidator : AbstractValidator<ResolveMemberLoginQuery>
{
    public ResolveMemberLoginQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");
    }
}
