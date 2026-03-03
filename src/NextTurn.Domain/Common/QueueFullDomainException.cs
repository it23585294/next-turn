namespace NextTurn.Domain.Common;

/// <summary>
/// Thrown when a user attempts to join a queue that has reached its maximum capacity.
///
/// Handled separately from <see cref="ConflictDomainException"/> because the 409 response
/// body carries a machine-readable flag (<c>{ canBookAppointment: true }</c>) that lets
/// the frontend offer an appointment fallback CTA rather than just showing an error message.
///
/// Caught by <c>DomainExceptionMiddleware</c> and mapped to HTTP 409 Conflict with body:
/// <code>{ "canBookAppointment": true }</code>
/// </summary>
public class QueueFullDomainException : DomainException
{
    public QueueFullDomainException() : base("The queue is currently full.") { }
}
