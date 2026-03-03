namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Abstraction over queue state reads — designed as a caching seam.
///
/// Sprint 1: <see cref="StubQueueStateService"/> derives state from direct SQL queries
/// via <c>IQueueRepository</c> — correct but not cached.
///
/// Sprint 2+: Replace the stub with a Redis-backed implementation that maintains a
/// sorted set per queue. Handlers and consumers never change because they depend on
/// this interface, not the concrete class.
///
/// Why a separate service and not just IQueueRepository?
///   IQueueRepository owns writes and aggregate-level reads (GetByIdAsync, HasActiveEntry).
///   IQueueStateService owns the derived, read-optimised views (position, ETA) that may
///   eventually be served from a cache. Separating the concerns keeps each interface
///   focused and makes the caching swap localised to a single class.
/// </summary>
public interface IQueueStateService
{
    /// <summary>
    /// Returns the current 1-based position of the given entry in the queue.
    /// Position 1 means the user is next to be served.
    ///
    /// Sprint 1: COUNT(entries WHERE QueueId = queueId AND Status IN (Waiting, Serving)
    ///           AND Id &lt;= entryId ordered by JoinedAt).
    /// Sprint 2+: ZRANK on a Redis sorted set keyed by queue ID.
    /// </summary>
    Task<int> GetPositionAsync(Guid queueId, Guid entryId, CancellationToken cancellationToken);
}
