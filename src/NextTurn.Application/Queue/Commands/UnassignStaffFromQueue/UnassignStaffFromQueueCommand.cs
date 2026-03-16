using MediatR;

namespace NextTurn.Application.Queue.Commands.UnassignStaffFromQueue;

public sealed record UnassignStaffFromQueueCommand(Guid QueueId, Guid StaffUserId) : IRequest<Unit>;
