using MediatR;

namespace NextTurn.Application.Organisation.Queries.ResolveMemberLogin;

public sealed record ResolveMemberLoginQuery(string Email)
    : IRequest<IReadOnlyList<MemberWorkspaceOption>>;
