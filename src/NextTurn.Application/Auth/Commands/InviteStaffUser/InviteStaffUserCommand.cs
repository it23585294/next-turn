using MediatR;

namespace NextTurn.Application.Auth.Commands.InviteStaffUser;

public sealed record InviteStaffUserCommand(
    string Name,
    string Email,
    string? Phone)
    : IRequest<InviteStaffUserResult>;
