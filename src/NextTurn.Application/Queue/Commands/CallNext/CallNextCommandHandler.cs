using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Commands.CallNext;

public sealed class CallNextCommandHandler : IRequestHandler<CallNextCommand, QueueEntryActionResult>
{
    private const string OneServingIndexName = "UX_QueueEntries_QueueId_OneServing";

    private readonly IQueueRepository _queueRepository;
    private readonly IApplicationDbContext _context;

    public CallNextCommandHandler(
        IQueueRepository queueRepository,
        IApplicationDbContext context)
    {
        _queueRepository = queueRepository;
        _context = context;
    }

    public async Task<QueueEntryActionResult> Handle(
        CallNextCommand command,
        CancellationToken cancellationToken)
    {
        var queue = await _queueRepository.GetByIdAsync(command.QueueId, cancellationToken);
        if (queue is null)
            throw new DomainException("Queue not found.");

        var currentServing = await _queueRepository.GetCurrentServingEntryAsync(command.QueueId, cancellationToken);
        if (currentServing is not null)
            throw new ConflictDomainException("A ticket is already being served.");

        var nextWaiting = await _queueRepository.GetNextWaitingEntryAsync(command.QueueId, cancellationToken);
        if (nextWaiting is null)
            throw new DomainException("No waiting entries in this queue.");

        nextWaiting.StartServing();

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsOneServingConflict(ex))
        {
            throw new ConflictDomainException("A ticket is already being served.");
        }

        return new QueueEntryActionResult(nextWaiting.Id, nextWaiting.TicketNumber, nextWaiting.Status.ToString());
    }

    private static bool IsOneServingConflict(DbUpdateException ex)
    {
        return ex.ToString().Contains(OneServingIndexName, StringComparison.OrdinalIgnoreCase);
    }
}
