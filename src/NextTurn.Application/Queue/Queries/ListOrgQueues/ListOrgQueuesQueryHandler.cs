using MediatR;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Queries.ListOrgQueues;

/// <summary>
/// Handles <see cref="ListOrgQueuesQuery"/> — returns all queues for the org admin's
/// organisation, with pre-computed shareable links.
///
/// No paging in Sprint 2 — orgs are unlikely to have enough queues to warrant it.
/// Returns an empty list if the organisation has no queues (not an error).
/// </summary>
public class ListOrgQueuesQueryHandler
    : IRequestHandler<ListOrgQueuesQuery, IReadOnlyList<OrgQueueSummary>>
{
    private readonly IQueueRepository _queueRepository;

    public ListOrgQueuesQueryHandler(IQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<IReadOnlyList<OrgQueueSummary>> Handle(
        ListOrgQueuesQuery query,
        CancellationToken  cancellationToken)
    {
        var queues = await _queueRepository.GetByOrganisationIdAsync(
            query.OrganisationId, cancellationToken);

        return queues
            .Select(q => new OrgQueueSummary(
                QueueId:                  q.Id,
                Name:                     q.Name,
                MaxCapacity:              q.MaxCapacity,
                AverageServiceTimeSeconds: q.AverageServiceTimeSeconds,
                Status:                   q.Status.ToString(),
                ShareableLink:            $"/queues/{query.OrganisationId}/{q.Id}"))
            .ToList();
    }
}
