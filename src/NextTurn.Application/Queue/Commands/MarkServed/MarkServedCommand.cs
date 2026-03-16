using MediatR;
using NextTurn.Application.Queue.Commands;

namespace NextTurn.Application.Queue.Commands.MarkServed;

public sealed record MarkServedCommand(Guid QueueId) : IRequest<QueueEntryActionResult>;
