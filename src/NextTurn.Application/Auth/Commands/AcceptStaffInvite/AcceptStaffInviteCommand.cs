using MediatR;

namespace NextTurn.Application.Auth.Commands.AcceptStaffInvite;

public sealed record AcceptStaffInviteCommand(
    string Token,
    string Password)
    : IRequest<Unit>;
