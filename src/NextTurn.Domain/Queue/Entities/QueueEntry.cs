using NextTurn.Domain.Queue.Enums;
using NextTurn.Domain.Common;

namespace NextTurn.Domain.Queue.Entities;

/// <summary>
/// Represents a single user's entry (ticket) in a queue.
///
/// This is a child entity of the Queue aggregate. It is accessed and persisted
/// only through the Queue aggregate's repository — never queried in isolation
/// without a queue context.
///
/// Invariants enforced by the factory:
///   - TicketNumber must be ≥ 1
/// </summary>
public class QueueEntry
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid              Id           { get; }
    public Guid              QueueId      { get; private set; }
    public Guid              UserId       { get; private set; }

    /// <summary>
    /// The human-readable ticket number shown to the user (e.g. displayed as "Ticket #42").
    /// Unique within a queue for a given day / session; assigned sequentially by the handler.
    /// </summary>
    public int               TicketNumber { get; private set; }

    public QueueEntryStatus  Status       { get; private set; }
    public DateTimeOffset    JoinedAt     { get; }

    // Required by EF Core for entity materialisation.
    protected QueueEntry() { } // No reference-type properties — no CS8618 suppression needed.

    private QueueEntry(
        Guid             id,
        Guid             queueId,
        Guid             userId,
        int              ticketNumber,
        QueueEntryStatus status,
        DateTimeOffset   joinedAt)
    {
        Id           = id;
        QueueId      = queueId;
        UserId       = userId;
        TicketNumber = ticketNumber;
        Status       = status;
        JoinedAt     = joinedAt;
    }

    /// <summary>
    /// Creates a new queue entry in <see cref="QueueEntryStatus.Waiting"/> status.
    /// </summary>
    /// <param name="queueId">The queue this entry belongs to.</param>
    /// <param name="userId">The user claiming this ticket.</param>
    /// <param name="ticketNumber">
    /// Sequential ticket number assigned by the handler via
    /// <c>IQueueRepository.GetNextTicketNumberAsync</c> — kept out of the domain
    /// model to avoid an async dependency inside a factory method.
    /// </param>
    public static QueueEntry Create(Guid queueId, Guid userId, int ticketNumber)
    {
        if (ticketNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(ticketNumber), "Ticket number must be at least 1.");

        return new QueueEntry(
            id:           Guid.NewGuid(),
            queueId:      queueId,
            userId:       userId,
            ticketNumber: ticketNumber,
            status:       QueueEntryStatus.Waiting,
            joinedAt:     DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Transitions the entry to <see cref="QueueEntryStatus.Cancelled"/> status.
    /// Represents a user voluntarily leaving the queue.
    /// 
    /// Invariant: The entry must be in <see cref="QueueEntryStatus.Waiting"/> or
    /// <see cref="QueueEntryStatus.Serving"/> status to be cancellable.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown if the entry is already in a terminal state (Served, Cancelled, NoShow).
    /// </exception>
    public void Cancel()
    {
        if (Status != QueueEntryStatus.Waiting && Status != QueueEntryStatus.Serving)
            throw new DomainException("Queue entry is already in a terminal state and cannot be cancelled.");

        Status = QueueEntryStatus.Cancelled;
    }

    /// <summary>
    /// Marks a waiting ticket as currently being served.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when the entry is not in <see cref="QueueEntryStatus.Waiting"/>.
    /// </exception>
    public void StartServing()
    {
        if (Status != QueueEntryStatus.Waiting)
            throw new DomainException("Only waiting queue entries can be moved to serving.");

        Status = QueueEntryStatus.Serving;
    }

    /// <summary>
    /// Marks a currently serving ticket as served.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when the entry is not in <see cref="QueueEntryStatus.Serving"/>.
    /// </exception>
    public void MarkServed()
    {
        if (Status != QueueEntryStatus.Serving)
            throw new DomainException("Only a serving queue entry can be marked as served.");

        Status = QueueEntryStatus.Served;
    }

    /// <summary>
    /// Marks a currently serving ticket as no-show.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when the entry is not in <see cref="QueueEntryStatus.Serving"/>.
    /// </exception>
    public void MarkNoShow()
    {
        if (Status != QueueEntryStatus.Serving)
            throw new DomainException("Only a serving queue entry can be marked as no-show.");

        Status = QueueEntryStatus.NoShow;
    }
}
