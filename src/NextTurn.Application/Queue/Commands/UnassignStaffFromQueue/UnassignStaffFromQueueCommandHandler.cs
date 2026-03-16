using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Commands.UnassignStaffFromQueue;

public sealed class UnassignStaffFromQueueCommandHandler : IRequestHandler<UnassignStaffFromQueueCommand, Unit>
{
    private readonly IQueueRepository _queueRepository;
    private readonly IApplicationDbContext _context;

    public UnassignStaffFromQueueCommandHandler(
        IQueueRepository queueRepository,
        IApplicationDbContext context)
    {
        _queueRepository = queueRepository;
        _context = context;
    }

    public async Task<Unit> Handle(UnassignStaffFromQueueCommand request, CancellationToken cancellationToken)
    {
        var queue = await _queueRepository.GetByIdAsync(request.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        await _queueRepository.RemoveStaffAssignmentAsync(
            request.QueueId,
            request.StaffUserId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
