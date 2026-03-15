using MediatR;
using NextTurn.Application.Queue.Commands;

namespace NextTurn.Application.Queue.Commands.CallNext;

public sealed record CallNextCommand(Guid QueueId) : IRequest<QueueEntryActionResult>;
