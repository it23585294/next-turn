using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Commands.MarkNoShow;

public sealed class MarkNoShowCommandHandler : IRequestHandler<MarkNoShowCommand, QueueEntryActionResult>
{
    private readonly IQueueRepository _queueRepository;
    private readonly IApplicationDbContext _context;

    public MarkNoShowCommandHandler(
        IQueueRepository queueRepository,
        IApplicationDbContext context)
    {
        _queueRepository = queueRepository;
        _context = context;
    }

    public async Task<QueueEntryActionResult> Handle(
        MarkNoShowCommand command,
        CancellationToken cancellationToken)
    {
        var queue = await _queueRepository.GetByIdAsync(command.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        var servingEntry = await _queueRepository.GetCurrentServingEntryAsync(command.QueueId, cancellationToken);
        if (servingEntry is null)
            throw new DomainException("No entry is currently being served.");

        servingEntry.MarkNoShow();
        await _context.SaveChangesAsync(cancellationToken);

        return new QueueEntryActionResult(servingEntry.Id, servingEntry.TicketNumber, servingEntry.Status.ToString());
    }
}
