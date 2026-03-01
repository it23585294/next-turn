namespace NextTurn.Application.Organisation.Commands.RegisterOrganisation;

/// <summary>
/// Returned by RegisterOrganisationCommandHandler on success.
/// Contains the IDs of both newly-created records so the API layer can
/// include them in the 201 Created response body.
/// </summary>
public record RegisterOrganisationResult(
    Guid OrganisationId,
    Guid AdminUserId
);
