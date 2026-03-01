using MediatR;
using Microsoft.Extensions.Logging;

namespace NextTurn.Application.Auth.Commands.RegisterUser;

/// <summary>
/// Handles UserRegisteredNotification — the in-process domain event published
/// immediately after a user account is created.
///
/// Sprint 1 scope (NT-10): Stub implementation — logs the event and records it
/// as a deferred action. Actual email/SMS delivery is intentionally out of scope
/// for this story and is tracked as NT-XX (Notifications epic, Sprint 2).
///
/// Why a stub rather than nothing?
///   The acceptance criterion states "a confirmation email/SMS is queued".
///   This handler closes the technical gap: the notification is handled,
///   the intent is recorded in logs, and the wiring is in place so Sprint 2
///   only needs to replace this body — no structural changes required.
///
/// Architecture note (Modular Monolith):
///   This handler lives in the Application layer alongside the command that
///   publishes it. MediatR discovers it automatically via assembly scanning in
///   AddApplication() → RegisterServicesFromAssembly(). No manual DI registration
///   is needed. In a future microservices migration this would become a subscriber
///   on an Azure Service Bus topic.
/// </summary>
public sealed class UserRegisteredNotificationHandler
    : INotificationHandler<UserRegisteredNotification>
{
    private readonly ILogger<UserRegisteredNotificationHandler> _logger;

    public UserRegisteredNotificationHandler(
        ILogger<UserRegisteredNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        UserRegisteredNotification notification,
        CancellationToken cancellationToken)
    {
        // TODO (NT-XX Sprint 2): Replace with real notification delivery.
        // Options considered:
        //   a) Transactional Outbox — write to OutboxMessages table, background
        //      worker polls and sends via SendGrid / Twilio.
        //   b) Direct SMTP — simple but risks losing the email if the call fails
        //      after SaveChanges. Outbox is more reliable.
        //   c) Azure Communication Services — aligns with Azure App Service deployment.
        //
        // For Sprint 1 the notification is acknowledged and logged. This ensures
        // the MediatR pipeline completes cleanly and the log provides an audit trail.
        _logger.LogInformation(
            "UserRegistered event received — UserId: {UserId}, Email: {Email}. " +
            "Email/SMS delivery deferred to Sprint 2 (NT-XX).",
            notification.UserId,
            notification.Email);

        return Task.CompletedTask;
    }
}
