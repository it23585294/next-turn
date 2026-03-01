using MediatR;

namespace NextTurn.Application.Auth.Commands.RegisterUser;

/// <summary>
/// Command representing the intent to register a new user.
/// Carries raw input from the API layer — validation and domain object construction
/// happen inside the handler, not here.
/// </summary>
public record RegisterUserCommand(
    string Name,
    string Email,
    string? Phone,
    string Password
) : IRequest<Unit>;
