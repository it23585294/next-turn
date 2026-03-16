namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationLogin;

public sealed record ResolveOrganisationLoginResult(
    Guid OrganisationId,
    string OrganisationName,
    string LoginPath);
