using MediatR;
using NextTurn.Application.Queue.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Queries.ListQueueStaffAssignments;

public sealed class ListQueueStaffAssignmentsQueryHandler
    : IRequestHandler<ListQueueStaffAssignmentsQuery, IReadOnlyList<QueueStaffAssignmentDto>>
{
    private readonly IQueueRepository _queueRepository;

    public ListQueueStaffAssignmentsQueryHandler(IQueueRepository queueRepository)
    {
        _queueRepository = queueRepository;
    }

    public async Task<IReadOnlyList<QueueStaffAssignmentDto>> Handle(
        ListQueueStaffAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var assigned = await _queueRepository.GetStaffAssignmentsAsync(request.QueueId, cancellationToken);

        return assigned
            .Select(a => new QueueStaffAssignmentDto(a.StaffUserId, a.Name, a.Email, a.IsActive))
            .ToList();
    }
}
