using MediatR;

namespace NextTurn.Application.Auth.Commands.ReactivateStaffUser;

public sealed record ReactivateStaffUserCommand(Guid UserId) : IRequest<Unit>;
