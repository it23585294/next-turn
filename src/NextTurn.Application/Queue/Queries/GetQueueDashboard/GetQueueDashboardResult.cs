namespace NextTurn.Application.Queue.Queries.GetQueueDashboard;

public sealed record QueueDashboardEntry(
    Guid EntryId,
    int TicketNumber,
    DateTimeOffset JoinedAt);

public sealed record GetQueueDashboardResult(
    Guid QueueId,
    string QueueName,
    string QueueStatus,
    int WaitingCount,
    QueueDashboardEntry? CurrentlyServing,
    IReadOnlyList<QueueDashboardEntry> WaitingEntries);
