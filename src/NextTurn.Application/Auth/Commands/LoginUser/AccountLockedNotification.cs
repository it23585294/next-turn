using MediatR;

namespace NextTurn.Application.Auth.Commands.LoginUser;

/// <summary>
/// In-process event published when a user account is temporarily locked out
/// after reaching the failed login threshold (3 attempts within a session).
///
/// Sprint 1: handled by AccountLockedNotificationHandler (stub — logs only).
/// Sprint 2 (NT-XX): the handler should send a notification to the user
///   (e.g. "Your account has been locked. Try again after {LockoutUntil}.")
///   via email or SMS using the Transactional Outbox pattern.
/// </summary>
public record AccountLockedNotification(
    Guid UserId,
    string Email,
    DateTimeOffset LockoutUntil
) : INotification;
