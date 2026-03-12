namespace NextTurn.Application.Queue.Queries.GetMyQueues;

/// <summary>
/// Lightweight projection of a queue the user is currently active in.
/// </summary>
public record MyQueueEntry(
    Guid   QueueId,
    Guid   OrganisationId,
    string QueueName,
    int    TicketNumber,
    string QueueStatus);
