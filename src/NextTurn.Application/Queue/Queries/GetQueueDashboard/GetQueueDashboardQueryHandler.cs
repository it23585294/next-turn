using MediatR;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Queries.GetQueueDashboard;

public sealed class GetQueueDashboardQueryHandler
    : IRequestHandler<GetQueueDashboardQuery, GetQueueDashboardResult>
{
    private readonly IQueueRepository _queueRepository;

    public GetQueueDashboardQueryHandler(IQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<GetQueueDashboardResult> Handle(
        GetQueueDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var queue = await _queueRepository.GetByIdAsync(query.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        var currentServing = await _queueRepository.GetCurrentServingEntryAsync(query.QueueId, cancellationToken);
        var waitingEntries = await _queueRepository.GetWaitingEntriesAsync(query.QueueId, cancellationToken);

        return new GetQueueDashboardResult(
            QueueId: query.QueueId,
            QueueName: queue.Name,
            QueueStatus: queue.Status.ToString(),
            WaitingCount: waitingEntries.Count,
            CurrentlyServing: currentServing is null
                ? null
                : new QueueDashboardEntry(currentServing.Id, currentServing.TicketNumber, currentServing.JoinedAt),
            WaitingEntries: waitingEntries
                .Select(e => new QueueDashboardEntry(e.Id, e.TicketNumber, e.JoinedAt))
                .ToList());
    }
}
