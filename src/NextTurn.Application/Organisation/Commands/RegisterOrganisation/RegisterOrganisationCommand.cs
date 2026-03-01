using MediatR;

namespace NextTurn.Application.Organisation.Commands.RegisterOrganisation;

/// <summary>
/// Command representing the intent to register a new organisation on the platform.
/// Carries raw input from the API layer — validation runs automatically via
/// ValidationBehavior in the MediatR pipeline before the handler is invoked.
/// </summary>
public record RegisterOrganisationCommand(
    string OrgName,
    string AddressLine1,
    string City,
    string PostalCode,
    string Country,
    string OrgType,
    string AdminName,
    string AdminEmail
) : IRequest<RegisterOrganisationResult>;
