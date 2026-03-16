using MediatR;
using NextTurn.Application.Queue.Queries.ListOrgQueues;

namespace NextTurn.Application.Queue.Queries.ListStaffQueues;

public sealed record ListStaffQueuesQuery(Guid UserId, string Role, Guid OrganisationId)
    : IRequest<IReadOnlyList<OrgQueueSummary>>;
