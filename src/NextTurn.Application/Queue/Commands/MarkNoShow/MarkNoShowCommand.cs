using MediatR;
using NextTurn.Application.Queue.Commands;

namespace NextTurn.Application.Queue.Commands.MarkNoShow;

public sealed record MarkNoShowCommand(Guid QueueId) : IRequest<QueueEntryActionResult>;
