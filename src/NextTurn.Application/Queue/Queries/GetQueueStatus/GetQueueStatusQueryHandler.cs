using MediatR;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Queries.GetQueueStatus;

/// <summary>
/// Handles <see cref="GetQueueStatusQuery"/> — returns the user's real-time position
/// and ETA inside a queue. Called by the frontend polling loop every 30 seconds.
///
/// 4-step flow:
///   1. Fetch the queue aggregate — DomainException("Queue not found.") if null.
///   2. Fetch the user's active entry — DomainException("No active queue entry found.")
///      if the user has not joined or their entry is no longer active.
///   3. Compute the user's real position: count of active entries with ticket ≤ user's ticket.
///      This correctly shrinks as entries ahead are served, unlike simple activeCount + 1.
///   4. Calculate ETA via <c>Queue.CalculateEtaSeconds(position)</c> and return result.
///
/// QueueStatus is included in the result so the frontend can show Paused/Closed banners.
/// </summary>
public class GetQueueStatusQueryHandler : IRequestHandler<GetQueueStatusQuery, GetQueueStatusResult>
{
    private readonly IQueueRepository _queueRepository;

    public GetQueueStatusQueryHandler(IQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<GetQueueStatusResult> Handle(
        GetQueueStatusQuery query,
        CancellationToken   cancellationToken)
    {
        // Step 1 — load the queue aggregate (needed for ETA + status)
        var queue = await _queueRepository.GetByIdAsync(query.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        // Step 2 — find the user's active entry
        // Null means the user never joined or their entry is no longer active (Served, etc.)
        var entry = await _queueRepository.GetUserActiveEntryAsync(
            query.QueueId, query.UserId, cancellationToken);

        if (entry is null)
            throw new DomainException("No active queue entry found.");

        // Step 3 — compute real position
        // COUNT of active entries with TicketNumber <= user's TicketNumber.
        // Example: tickets 1, 2, 3 are Waiting; user has ticket 3 → position = 3.
        // After ticket 1 is Served: tickets 2, 3 still active → user moves to position 2.
        int position = await _queueRepository.GetUserPositionAsync(
            query.QueueId, entry.TicketNumber, cancellationToken);

        // Step 4 — calculate ETA and return
        int etaSeconds = queue.CalculateEtaSeconds(position);

        return new GetQueueStatusResult(
            TicketNumber:         entry.TicketNumber,
            PositionInQueue:      position,
            EstimatedWaitSeconds: etaSeconds,
            QueueStatus:          queue.Status);
    }
}
