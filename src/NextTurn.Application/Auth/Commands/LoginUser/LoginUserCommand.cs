using MediatR;
using NextTurn.Application.Auth;

namespace NextTurn.Application.Auth.Commands.LoginUser;

/// <summary>
/// Command representing the intent to authenticate an existing user.
/// Carries raw input from the API layer — validation happens via LoginUserValidator
/// in the MediatR pipeline before the handler is invoked.
/// </summary>
public record LoginUserCommand(
    string Email,
    string Password
) : IRequest<LoginResult>;
