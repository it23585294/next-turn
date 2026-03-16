using MediatR;

namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationTenant;

public sealed record ResolveOrganisationTenantQuery(string Slug)
    : IRequest<ResolveOrganisationTenantResult>;
