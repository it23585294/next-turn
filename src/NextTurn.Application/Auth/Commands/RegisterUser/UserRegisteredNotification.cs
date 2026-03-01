using MediatR;

namespace NextTurn.Application.Auth.Commands.RegisterUser;

/// <summary>
/// In-process domain event published after a user is successfully registered.
/// Any number of INotificationHandler implementations can react to this
/// (e.g. sending a welcome email) without the handler knowing about them.
/// </summary>
public record UserRegisteredNotification(
    Guid UserId,
    string Email
) : INotification;
