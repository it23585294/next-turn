using MediatR;

namespace NextTurn.Application.Queue.Commands.AssignStaffToQueue;

public sealed record AssignStaffToQueueCommand(Guid QueueId, Guid StaffUserId) : IRequest<Unit>;
