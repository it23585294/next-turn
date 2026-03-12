using MediatR;
using NextTurn.Application.Auth;

namespace NextTurn.Application.Auth.Commands.LoginGlobalUser;

/// <summary>
/// Command for authenticating a consumer (global) user who is NOT bound to any org.
/// No X-Tenant-Id header is required for this flow.
/// </summary>
public record LoginGlobalUserCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;
