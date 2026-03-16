using MediatR;

namespace NextTurn.Application.Auth.Queries.ListStaffUsers;

public sealed record ListStaffUsersQuery : IRequest<IReadOnlyList<StaffUserSummary>>;
