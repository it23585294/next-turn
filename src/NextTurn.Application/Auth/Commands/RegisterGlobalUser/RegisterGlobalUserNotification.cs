using MediatR;

namespace NextTurn.Application.Auth.Commands.RegisterGlobalUser;

/// <summary>
/// In-process domain event published after a consumer (global) user is registered.
/// </summary>
public record RegisterGlobalUserNotification(
    Guid UserId,
    string Email
) : INotification;
