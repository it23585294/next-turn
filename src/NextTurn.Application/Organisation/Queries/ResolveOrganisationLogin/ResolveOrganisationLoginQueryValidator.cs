using FluentValidation;

namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationLogin;

public sealed class ResolveOrganisationLoginQueryValidator : AbstractValidator<ResolveOrganisationLoginQuery>
{
    public ResolveOrganisationLoginQueryValidator()
    {
        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("Admin email format is invalid.");
    }
}
