namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationTenant;

public sealed record ResolveOrganisationTenantResult(
    Guid OrganisationId,
    string OrganisationName,
    string Slug);
