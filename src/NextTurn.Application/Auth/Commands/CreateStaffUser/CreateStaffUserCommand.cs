using MediatR;

namespace NextTurn.Application.Auth.Commands.CreateStaffUser;

public sealed record CreateStaffUserCommand(
    string Name,
    string Email,
    string? Phone,
    string Password) : IRequest<Unit>;
