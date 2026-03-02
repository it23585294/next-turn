using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Queue.Entities;
using NextTurn.Domain.Queue.Enums;
using NextTurn.Domain.Queue.Repositories;
using NextTurn.Infrastructure.Persistence;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.Infrastructure.Queue;

/// <summary>
/// EF Core implementation of <see cref="IQueueRepository"/>.
///
/// All methods that query QueueEntry filter by an explicit <c>queueId</c> parameter.
/// This is a deliberate redundancy alongside the global query filter — if a caller
/// accidentally bypasses the filter (e.g. via IgnoreQueryFilters), the queueId
/// still scopes the result correctly.
///
/// None of the write methods call SaveChangesAsync. The caller (command handler)
/// is responsible for committing the unit of work in a single SaveChangesAsync call
/// after all mutations for the request are staged — the same pattern used in
/// OrganisationRepository and UserRepository.
/// </summary>
public sealed class QueueRepository : IQueueRepository
{
    private readonly ApplicationDbContext _context;

    public QueueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<QueueEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Queues
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetActiveEntryCountAsync(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        return await _context.QueueEntries
            .CountAsync(
                e => e.QueueId == queueId &&
                     (e.Status == QueueEntryStatus.Waiting || e.Status == QueueEntryStatus.Serving),
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetNextTicketNumberAsync(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        // MAX returns null when there are no rows — the ?? 0 + 1 yields 1 for the first entry.
        int? max = await _context.QueueEntries
            .Where(e => e.QueueId == queueId)
            .MaxAsync(e => (int?)e.TicketNumber, cancellationToken);

        return (max ?? 0) + 1;
    }

    /// <inheritdoc/>
    public async Task AddEntryAsync(
        QueueEntry entry,
        CancellationToken cancellationToken)
    {
        // Stage for insertion only — caller commits via SaveChangesAsync.
        await _context.QueueEntries.AddAsync(entry, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasActiveEntryAsync(
        Guid queueId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.QueueEntries
            .AnyAsync(
                e => e.QueueId == queueId &&
                     e.UserId   == userId  &&
                     (e.Status == QueueEntryStatus.Waiting || e.Status == QueueEntryStatus.Serving),
                cancellationToken);
    }
}
