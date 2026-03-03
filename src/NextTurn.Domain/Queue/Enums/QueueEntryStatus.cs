namespace NextTurn.Domain.Queue.Enums;

/// <summary>
/// Represents the lifecycle state of a single queue entry (one user's ticket).
///
/// Lifecycle:
///   Waiting  → Serving   (counter calls the ticket)
///   Serving  → Served    (service completed)
///   Serving  → NoShow    (user did not present when called)
///   Waiting  → Cancelled (user voluntarily leaves the queue)
/// </summary>
public enum QueueEntryStatus
{
    /// <summary>The user is waiting for their turn.</summary>
    Waiting,

    /// <summary>The user is currently being served at a counter.</summary>
    Serving,

    /// <summary>Service has been completed for this entry.</summary>
    Served,

    /// <summary>The user cancelled their entry before being called.</summary>
    Cancelled,

    /// <summary>The user's ticket was called but they did not present themselves.</summary>
    NoShow
}
