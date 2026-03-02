using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Domain.Queue.Entities;

/// <summary>
/// Aggregate root representing a queue owned by an organisation.
///
/// Invariants enforced by the factory:
///   - Name is non-empty and ≤ 200 characters
///   - MaxCapacity is at least 1
///   - AverageServiceTimeSeconds is at least 1
///
/// Queue state reads (active count, position, ETA) are NOT computed inside the
/// aggregate — they require async repository queries and belong in the handler.
/// The aggregate exposes pure, synchronous behaviour methods that the handler
/// calls after loading the data it needs.
/// </summary>
public class Queue
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid         Id                      { get; }
    public Guid         OrganisationId          { get; private set; }
    public string       Name                    { get; private set; }
    public int          MaxCapacity             { get; private set; }

    /// <summary>
    /// Average number of seconds spent serving a single customer.
    /// Used to estimate wait times: ETA = position × AverageServiceTimeSeconds.
    /// </summary>
    public int          AverageServiceTimeSeconds { get; private set; }

    public QueueStatus  Status                  { get; private set; }
    public DateTimeOffset CreatedAt             { get; }

    // Required by EF Core for entity materialisation — prevents accidental direct construction.
    protected Queue()
    {
        // default! suppresses CS8618 — EF Core assigns this before the instance is ever read.
        Name = default!;
    }

    private Queue(
        Guid          id,
        Guid          organisationId,
        string        name,
        int           maxCapacity,
        int           averageServiceTimeSeconds,
        QueueStatus   status,
        DateTimeOffset createdAt)
    {
        Id                        = id;
        OrganisationId            = organisationId;
        Name                      = name;
        MaxCapacity               = maxCapacity;
        AverageServiceTimeSeconds = averageServiceTimeSeconds;
        Status                    = status;
        CreatedAt                 = createdAt;
    }

    /// <summary>
    /// Creates a new <see cref="Queue"/> in <see cref="QueueStatus.Active"/> status.
    /// </summary>
    /// <param name="organisationId">The owning organisation.</param>
    /// <param name="name">Human-readable queue name displayed to users.</param>
    /// <param name="maxCapacity">Maximum number of simultaneous active entries (Waiting + Serving).</param>
    /// <param name="averageServiceTimeSeconds">Seconds per customer — drives ETA calculations.</param>
    public static Queue Create(
        Guid   organisationId,
        string name,
        int    maxCapacity,
        int    averageServiceTimeSeconds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Queue name is required.");

        if (name.Length > 200)
            throw new DomainException("Queue name must not exceed 200 characters.");

        if (maxCapacity < 1)
            throw new DomainException("Queue capacity must be at least 1.");

        if (averageServiceTimeSeconds < 1)
            throw new DomainException("Average service time must be at least 1 second.");

        return new Queue(
            id:                        Guid.NewGuid(),
            organisationId:            organisationId,
            name:                      name.Trim(),
            maxCapacity:               maxCapacity,
            averageServiceTimeSeconds: averageServiceTimeSeconds,
            status:                    QueueStatus.Active,
            createdAt:                 DateTimeOffset.UtcNow);
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if the queue can accept a new entry given the current
    /// number of active entries (Waiting + Serving).
    /// </summary>
    /// <param name="activeCount">
    /// Queried by the handler via <c>IQueueRepository.GetActiveEntryCountAsync</c>
    /// before calling this method — keeps async I/O out of the domain model.
    /// </param>
    public bool CanAcceptEntry(int activeCount) => activeCount < MaxCapacity;

    /// <summary>
    /// Estimates the wait time in seconds for a user at the given 1-based position.
    /// Formula: position × AverageServiceTimeSeconds.
    /// </summary>
    /// <param name="position">1-based position in the waiting line.</param>
    public int CalculateEtaSeconds(int position) => position * AverageServiceTimeSeconds;
}
