using MediatR;
using Microsoft.Extensions.Logging;

namespace NextTurn.Application.Auth.Commands.RegisterGlobalUser;

/// <summary>
/// Handles RegisterGlobalUserNotification.
/// Sprint stub — logs the event; email delivery is deferred to the Notifications epic.
/// </summary>
public sealed class RegisterGlobalUserNotificationHandler
    : INotificationHandler<RegisterGlobalUserNotification>
{
    private readonly ILogger<RegisterGlobalUserNotificationHandler> _logger;

    public RegisterGlobalUserNotificationHandler(
        ILogger<RegisterGlobalUserNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(RegisterGlobalUserNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Consumer account registered — UserId: {UserId}, Email: {Email}. Welcome email deferred.",
            notification.UserId, notification.Email);

        return Task.CompletedTask;
    }
}
