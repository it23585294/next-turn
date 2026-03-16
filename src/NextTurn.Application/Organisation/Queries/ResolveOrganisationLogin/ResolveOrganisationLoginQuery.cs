using MediatR;

namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationLogin;

public sealed record ResolveOrganisationLoginQuery(string AdminEmail)
    : IRequest<ResolveOrganisationLoginResult>;
