using FluentValidation;

namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationTenant;

public sealed class ResolveOrganisationTenantQueryValidator : AbstractValidator<ResolveOrganisationTenantQuery>
{
    public ResolveOrganisationTenantQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Organisation slug is required.")
            .MinimumLength(3).WithMessage("Organisation slug is too short.")
            .MaximumLength(64).WithMessage("Organisation slug is too long.");
    }
}
