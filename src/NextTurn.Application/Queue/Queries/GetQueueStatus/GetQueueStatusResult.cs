using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Application.Queue.Queries.GetQueueStatus;

/// <summary>
/// Returned by <see cref="GetQueueStatusQueryHandler"/> for a user polling their queue status.
///
/// <para>
/// <b>TicketNumber</b> — the user's assigned ticket (e.g. #42). Displayed on the UI as
/// a persistent identifier regardless of how position changes.
/// </para>
/// <para>
/// <b>PositionInQueue</b> — 1-based real-time position. Computed as the count of active
/// entries with a ticket number ≤ the user's ticket. Position 1 means the user is next.
/// </para>
/// <para>
/// <b>EstimatedWaitSeconds</b> — ETA derived from
/// <c>Queue.CalculateEtaSeconds(position)</c>. The frontend converts to a human-readable
/// duration (e.g. "~5 min").
/// </para>
/// <para>
/// <b>QueueStatus</b> — current operational state of the queue. The frontend renders
/// a "Paused" or "Closed" banner when the queue is not <see cref="QueueStatus.Active"/>.
/// </para>
/// </summary>
public sealed record GetQueueStatusResult(
    int         TicketNumber,
    int         PositionInQueue,
    int         EstimatedWaitSeconds,
    QueueStatus QueueStatus);
