using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Commands.AssignStaffToQueue;

public sealed class AssignStaffToQueueCommandHandler : IRequestHandler<AssignStaffToQueueCommand, Unit>
{
    private readonly IQueueRepository _queueRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _context;

    public AssignStaffToQueueCommandHandler(
        IQueueRepository queueRepository,
        IUserRepository userRepository,
        IApplicationDbContext context)
    {
        _queueRepository = queueRepository;
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<Unit> Handle(AssignStaffToQueueCommand request, CancellationToken cancellationToken)
    {
        var queue = await _queueRepository.GetByIdAsync(request.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        var staffUser = await _userRepository.GetByIdAsync(request.StaffUserId, cancellationToken);
        if (staffUser is null)
            throw new DomainException("Staff user not found.");

        if (staffUser.Role != UserRole.Staff)
            throw new DomainException("Only staff accounts can be assigned to queues.");

        if (!staffUser.IsActive)
            throw new DomainException("Inactive staff accounts cannot be assigned.");

        if (staffUser.TenantId != queue.OrganisationId)
            throw new DomainException("Staff user belongs to a different organisation.");

        var alreadyAssigned = await _queueRepository.IsStaffAlreadyAssignedAsync(
            request.QueueId,
            request.StaffUserId,
            cancellationToken);

        if (alreadyAssigned)
            return Unit.Value;

        await _queueRepository.AddStaffAssignmentAsync(
            queue.OrganisationId,
            request.QueueId,
            request.StaffUserId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
