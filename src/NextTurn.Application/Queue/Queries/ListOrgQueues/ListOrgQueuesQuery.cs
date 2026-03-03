using MediatR;

namespace NextTurn.Application.Queue.Queries.ListOrgQueues;

/// <summary>
/// Query to list all queues owned by the specified organisation.
/// Used by the org admin dashboard to populate the queue list on page load.
/// OrganisationId is taken from the authenticated user's JWT <c>tid</c> claim.
/// </summary>
public record ListOrgQueuesQuery(Guid OrganisationId)
    : IRequest<IReadOnlyList<OrgQueueSummary>>;
