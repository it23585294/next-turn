using MediatR;

namespace NextTurn.Application.Queue.Queries.GetMyQueues;

/// <summary>
/// Returns all queues the current user has an active entry in.
/// Active means QueueEntryStatus is Waiting or Serving.
/// </summary>
public record GetMyQueuesQuery(Guid UserId) : IRequest<IReadOnlyList<MyQueueEntry>>;
