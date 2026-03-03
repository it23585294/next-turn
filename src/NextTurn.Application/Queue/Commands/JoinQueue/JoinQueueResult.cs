namespace NextTurn.Application.Queue.Commands.JoinQueue;

/// <summary>
/// Returned by <see cref="JoinQueueCommandHandler"/> on a successful queue join.
///
/// <para>
/// <b>TicketNumber</b> — the sequential number assigned to this entry within the queue.
/// Displayed prominently to the user (e.g. "Your ticket: #42").
/// </para>
/// <para>
/// <b>PositionInQueue</b> — 1-based position at the moment of joining.
/// Position 1 means the user is next to be served.
/// </para>
/// <para>
/// <b>EstimatedWaitSeconds</b> — ETA derived from
/// <c>Queue.CalculateEtaSeconds(position)</c>. The frontend converts this to a
/// human-readable duration (e.g. "~10 min").
/// </para>
/// </summary>
public sealed record JoinQueueResult(
    int TicketNumber,
    int PositionInQueue,
    int EstimatedWaitSeconds
);
