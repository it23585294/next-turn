using MediatR;

namespace NextTurn.Application.Auth.Commands.DeactivateStaffUser;

public sealed record DeactivateStaffUserCommand(Guid UserId) : IRequest<Unit>;
