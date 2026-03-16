using MediatR;
using NextTurn.Application.Queue.Common;

namespace NextTurn.Application.Queue.Queries.ListQueueStaffAssignments;

public sealed record ListQueueStaffAssignmentsQuery(Guid QueueId)
    : IRequest<IReadOnlyList<QueueStaffAssignmentDto>>;
