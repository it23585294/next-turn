using FluentAssertions;
using Moq;
using NextTurn.Application.Queue.Queries.GetQueueStatus;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Enums;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry  = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.UnitTests.Application.Queue;

/// <summary>
/// Unit tests for <see cref="GetQueueStatusQueryHandler"/>.
///
/// All dependencies are Moq doubles — no database, no HttpContext.
/// Tests verify the handler's 4-step orchestration logic in isolation.
///
/// Key invariants exercised:
///   - Happy path: PositionInQueue matches the value returned by GetUserPositionAsync
///   - Happy path: EstimatedWaitSeconds = position × AverageServiceTimeSeconds
///     (proving CalculateEtaSeconds is called with the correct position argument)
///   - Happy path: TicketNumber and QueueStatus are propagated to the result
///   - Position = 1 → ETA equals exactly one average service period ("You're next!")
///   - "Queue not found." → DomainException (step 1)
///   - "No active queue entry found." → DomainException (step 2)
/// </summary>
public sealed class GetQueueStatusQueryHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();

    private readonly GetQueueStatusQueryHandler _handler;

    // ── Shared test data ──────────────────────────────────────────────────────

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrgId   = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private const int AvgServiceTimeSecs = 300;  // 5 minutes per customer
    private const int DefaultTicketNumber = 7;
    private const int DefaultPosition     = 3;   // 3 entries ahead of / including the user

    public GetQueueStatusQueryHandlerTests()
    {
        // Default: queue exists, user has an active entry, position = 3
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue());

        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEntry(DefaultTicketNumber));

        _queueRepositoryMock
            .Setup(r => r.GetUserPositionAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DefaultPosition);

        _handler = new GetQueueStatusQueryHandler(_queueRepositoryMock.Object);
    }

    // ── PositionInQueue ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsPositionInQueue_FromGetUserPositionAsync()
    {
        _queueRepositoryMock
            .Setup(r => r.GetUserPositionAsync(QueueId, DefaultTicketNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.PositionInQueue.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenUserIsFirst_PositionInQueueIsOne()
    {
        // Simulates the "You're next!" state — user is at the front.
        _queueRepositoryMock
            .Setup(r => r.GetUserPositionAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.PositionInQueue.Should().Be(1);
    }

    // ── EstimatedWaitSeconds / CalculateEtaSeconds ────────────────────────────
    //
    // Queue.CalculateEtaSeconds(position) = position × AverageServiceTimeSeconds.
    // These tests verify that the handler passes the correct position to that method,
    // not a stale or cached value.

    [Fact]
    public async Task Handle_EstimatedWaitSeconds_IsPositionTimesAvgServiceTimeSecs()
    {
        // position = 4 → eta = 4 × 300 = 1200
        _queueRepositoryMock
            .Setup(r => r.GetUserPositionAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.EstimatedWaitSeconds.Should().Be(4 * AvgServiceTimeSecs);
    }

    [Fact]
    public async Task Handle_WhenUserIsFirst_EstimatedWaitSeconds_EqualsOneAvgServicePeriod()
    {
        // position = 1 → the user is being called next; ETA = 1 × avgServiceTime
        _queueRepositoryMock
            .Setup(r => r.GetUserPositionAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.EstimatedWaitSeconds.Should().Be(AvgServiceTimeSecs);
    }

    [Fact]
    public async Task Handle_EstimatedWaitSeconds_ScalesLinearlyWithPosition()
    {
        // position = 10 → eta = 10 × 300 = 3000
        _queueRepositoryMock
            .Setup(r => r.GetUserPositionAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.EstimatedWaitSeconds.Should().Be(10 * AvgServiceTimeSecs);
    }

    // ── TicketNumber ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsTicketNumber_FromActiveEntry()
    {
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEntry(ticketNumber: 42));

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.TicketNumber.Should().Be(42);
    }

    // ── QueueStatus – banners (Paused / Closed) ───────────────────────────────

    [Fact]
    public async Task Handle_ReturnsQueueStatus_ActiveQueue()
    {
        // Queue returned by the default setup has Status = Active
        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.QueueStatus.Should().Be(QueueStatus.Active);
    }

    [Fact]
    public async Task Handle_ReturnsQueueStatus_PausedQueue()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue(QueueStatus.Paused));

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.QueueStatus.Should().Be(QueueStatus.Paused);
    }

    [Fact]
    public async Task Handle_ReturnsQueueStatus_ClosedQueue()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue(QueueStatus.Closed));

        var result = await _handler.Handle(ValidQuery(), CancellationToken.None);

        result.QueueStatus.Should().Be(QueueStatus.Closed);
    }

    // ── GetUserPositionAsync called with correct arguments ────────────────────

    [Fact]
    public async Task Handle_CallsGetUserPositionAsync_WithCorrectQueueIdAndTicketNumber()
    {
        const int ticketNumber = 99;

        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(
                QueueId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEntry(ticketNumber));

        await _handler.Handle(ValidQuery(), CancellationToken.None);

        _queueRepositoryMock.Verify(
            r => r.GetUserPositionAsync(QueueId, ticketNumber, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Queue not found (step 1) ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenQueueNotFound_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var act = async () => await _handler.Handle(ValidQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenQueueNotFound_DoesNotCallGetUserActiveEntryAsync()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        try { await _handler.Handle(ValidQuery(), CancellationToken.None); } catch { /* expected */ }

        _queueRepositoryMock.Verify(
            r => r.GetUserActiveEntryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── No active entry (step 2) ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoActiveEntry_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        var act = async () => await _handler.Handle(ValidQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("No active queue entry found.");
    }

    [Fact]
    public async Task Handle_WhenNoActiveEntry_DoesNotCallGetUserPositionAsync()
    {
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        try { await _handler.Handle(ValidQuery(), CancellationToken.None); } catch { /* expected */ }

        _queueRepositoryMock.Verify(
            r => r.GetUserPositionAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GetQueueStatusQuery ValidQuery() =>
        new(QueueId, UserId);

    /// <summary>Builds a Queue aggregate in the given status (default: Active).</summary>
    private static QueueEntity BuildQueue(QueueStatus status = QueueStatus.Active)
    {
        // Queue.Create always produces an Active queue — for other statuses we'd need
        // to exercise the status-transition methods (not yet implemented in Sprint 2).
        // Active is the production status for a running queue, which covers all normal
        // happy-path and ETA tests. Paused/Closed variants are tested by using a second
        // Queue.Create + down-casting would require more domain work, so we confirm
        // status propagation for Active and use Paused/Closed after Queue gains those
        // transition methods in Sprint 3. For now, we cover Active + the status variants
        // via a private helper that reflectively sets the backing field.
        var queue = QueueEntity.Create(
            organisationId:            OrgId,
            name:                      "Service Counter",
            maxCapacity:               100,
            averageServiceTimeSeconds: AvgServiceTimeSecs);

        if (status != QueueStatus.Active)
            SetQueueStatus(queue, status);

        return queue;
    }

    /// <summary>Builds a QueueEntry with the given ticket number.</summary>
    private static QueueEntry BuildEntry(int ticketNumber) =>
        QueueEntry.Create(QueueId, UserId, ticketNumber);

    /// <summary>
    /// Sets the <c>Status</c> backing field on a <see cref="QueueEntity"/> via
    /// reflection. Used only to construct test doubles for Paused/Closed banner
    /// tests — the status-transition domain methods are a Sprint 3 story.
    /// </summary>
    private static void SetQueueStatus(QueueEntity queue, QueueStatus status)
    {
        // Queue.Status has a private setter — no public transition method yet.
        // Reflection is the least-invasive approach for test-only status overrides.
        var prop = typeof(QueueEntity).GetProperty(nameof(QueueEntity.Status))!;
        prop.SetValue(queue, status);
    }
}
