using MediatR;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Queries.GetMyQueues;

/// <summary>
/// Returns all queues the authenticated user currently has an active entry in.
/// </summary>
public class GetMyQueuesQueryHandler
    : IRequestHandler<GetMyQueuesQuery, IReadOnlyList<MyQueueEntry>>
{
    private readonly IQueueRepository _queueRepository;

    public GetMyQueuesQueryHandler(IQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<IReadOnlyList<MyQueueEntry>> Handle(
        GetMyQueuesQuery  query,
        CancellationToken cancellationToken)
    {
        var entries = await _queueRepository
            .GetUserActiveEntriesAsync(query.UserId, cancellationToken);

        return entries
            .Select(e => new MyQueueEntry(e.QueueId, e.QueueName, e.TicketNumber, e.Status))
            .ToList();
    }
}
