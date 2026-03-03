namespace NextTurn.Domain.Queue.Enums;

/// <summary>
/// Represents the operational state of a queue.
///
/// Lifecycle:
///   Active  → Paused  (org pauses intake — no new entries accepted)
///   Active  → Closed  (queue is done for the day / permanently closed)
///   Paused  → Active  (org resumes intake)
///   Paused  → Closed
/// </summary>
public enum QueueStatus
{
    /// <summary>The queue is open and accepting new entries.</summary>
    Active,

    /// <summary>The queue is temporarily paused; no new entries are accepted, but existing entries are retained.</summary>
    Paused,

    /// <summary>The queue is permanently closed for the session or decommissioned.</summary>
    Closed
}
