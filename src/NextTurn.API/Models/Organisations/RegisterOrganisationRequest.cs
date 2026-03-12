namespace NextTurn.API.Models.Organisations;

/// <summary>
/// Request body for POST /api/organisations.
/// Raw input is mapped to RegisterOrganisationCommand and validated by
/// RegisterOrganisationCommandValidator before the handler is invoked.
/// </summary>
public record RegisterOrganisationRequest(
    string OrgName,
    string AddressLine1,
    string City,
    string PostalCode,
    string Country,
    string OrgType,
    string AdminName,
    string AdminEmail
);
