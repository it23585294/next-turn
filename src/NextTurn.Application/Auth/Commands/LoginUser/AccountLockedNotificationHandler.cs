using MediatR;
using Microsoft.Extensions.Logging;

namespace NextTurn.Application.Auth.Commands.LoginUser;

/// <summary>
/// Sprint 1 stub handler for AccountLockedNotification.
///
/// Currently only logs the event. The actual notification delivery (email / SMS)
/// is deferred to Sprint 2 (NT-XX) and will be implemented using the
/// Transactional Outbox pattern with Azure Communication Services or SendGrid.
///
/// MediatR discovers this handler automatically via RegisterServicesFromAssembly()
/// in AddApplication() — no extra DI registration needed.
/// </summary>
public sealed class AccountLockedNotificationHandler
    : INotificationHandler<AccountLockedNotification>
{
    private readonly ILogger<AccountLockedNotificationHandler> _logger;

    public AccountLockedNotificationHandler(
        ILogger<AccountLockedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AccountLockedNotification notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "AccountLocked event received — UserId: {UserId}, Email: {Email}, " +
            "LockoutUntil: {LockoutUntil}. Account lockout notification delivery " +
            "deferred to Sprint 2 (NT-XX).",
            notification.UserId,
            notification.Email,
            notification.LockoutUntil);

        return Task.CompletedTask;
    }
}
