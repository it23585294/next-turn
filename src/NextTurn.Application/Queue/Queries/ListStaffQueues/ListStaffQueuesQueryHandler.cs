using MediatR;
using NextTurn.Application.Queue.Queries.ListOrgQueues;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Queries.ListStaffQueues;

public sealed class ListStaffQueuesQueryHandler
    : IRequestHandler<ListStaffQueuesQuery, IReadOnlyList<OrgQueueSummary>>
{
    private readonly IQueueRepository _queueRepository;

    public ListStaffQueuesQueryHandler(IQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<IReadOnlyList<OrgQueueSummary>> Handle(
        ListStaffQueuesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NextTurn.Domain.Queue.Entities.Queue> queues;

        if (request.Role == "Staff")
        {
            queues = await _queueRepository.GetQueuesAssignedToStaffAsync(request.UserId, cancellationToken);
        }
        else
        {
            queues = await _queueRepository.GetByOrganisationIdAsync(request.OrganisationId, cancellationToken);
        }

        return queues
            .Select(q => new OrgQueueSummary(
                QueueId: q.Id,
                Name: q.Name,
                MaxCapacity: q.MaxCapacity,
                AverageServiceTimeSeconds: q.AverageServiceTimeSeconds,
                Status: q.Status.ToString(),
                ShareableLink: $"/queues/{q.OrganisationId}/{q.Id}"))
            .ToList();
    }
}
