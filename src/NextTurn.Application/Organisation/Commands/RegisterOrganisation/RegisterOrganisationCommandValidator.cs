using FluentValidation;
using NextTurn.Domain.Organisation.Enums;

namespace NextTurn.Application.Organisation.Commands.RegisterOrganisation;

/// <summary>
/// Validates a RegisterOrganisationCommand before the handler processes it.
/// Runs automatically via ValidationBehavior in the MediatR pipeline.
/// A 422 response is returned if any rule fails (via ValidationExceptionMiddleware).
/// </summary>
public class RegisterOrganisationCommandValidator
    : AbstractValidator<RegisterOrganisationCommand>
{
    public RegisterOrganisationCommandValidator()
    {
        RuleFor(x => x.OrgName)
            .NotEmpty().WithMessage("Organisation name is required.")
            .MaximumLength(200).WithMessage("Organisation name must not exceed 200 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(300).WithMessage("Address line 1 must not exceed 300 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        RuleFor(x => x.OrgType)
            .NotEmpty().WithMessage("Organisation type is required.")
            .Must(BeAValidOrgType)
            .WithMessage($"Organisation type must be one of: " +
                         $"{string.Join(", ", Enum.GetNames<OrganisationType>())}.");

        RuleFor(x => x.AdminName)
            .NotEmpty().WithMessage("Admin name is required.")
            .MaximumLength(200).WithMessage("Admin name must not exceed 200 characters.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("Admin email format is invalid.")
            .MaximumLength(320).WithMessage("Admin email must not exceed 320 characters.");
    }

    private static bool BeAValidOrgType(string value) =>
        Enum.TryParse<OrganisationType>(value, ignoreCase: true, out _);
}
