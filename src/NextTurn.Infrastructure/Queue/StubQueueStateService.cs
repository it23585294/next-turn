using Microsoft.Extensions.Logging;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Infrastructure.Queue;

/// <summary>
/// Sprint 1 implementation of <see cref="IQueueStateService"/>.
///
/// Derives position by counting active entries that joined before the given entry
/// (ordered by JoinedAt). No cache is involved — every call hits SQL Server.
///
/// Sprint 2+: Replace this class with a Redis-backed implementation.
/// The swap requires only a DI registration change in
/// <c>Infrastructure.DependencyInjection</c> — no changes to callers.
/// </summary>
public sealed class StubQueueStateService : IQueueStateService
{
    private readonly IApplicationDbContext           _context;
    private readonly ILogger<StubQueueStateService> _logger;

    public StubQueueStateService(
        IApplicationDbContext          context,
        ILogger<StubQueueStateService> logger)
    {
        _context = context;
        _logger  = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Sprint 1: counts active entries with a JoinedAt timestamp ≤ the given entry's
    /// JoinedAt. This is a DB scan — acceptable for Sprint 1 volumes.
    /// Sprint 2+: ZRANK on a Redis sorted set eliminates the scan.
    /// </remarks>
    public async Task<int> GetPositionAsync(
        Guid              queueId,
        Guid              entryId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "[STUB] GetPositionAsync — deriving position from SQL for entry {EntryId} in queue {QueueId}.",
            entryId, queueId);

        // Load the target entry to get its JoinedAt timestamp.
        var targetEntry = await _context.QueueEntries
            .FindAsync([entryId], cancellationToken);

        if (targetEntry is null)
            return 1; // Fallback: entry not found, return positional default.

        // Count active entries that joined at or before this entry.
        var activeStatuses = new[] { QueueEntryStatus.Waiting, QueueEntryStatus.Serving };

        int position = _context.QueueEntries
            .Count(e =>
                e.QueueId == queueId &&
                activeStatuses.Contains(e.Status) &&
                e.JoinedAt <= targetEntry.JoinedAt);

        return Math.Max(position, 1);
    }
}
