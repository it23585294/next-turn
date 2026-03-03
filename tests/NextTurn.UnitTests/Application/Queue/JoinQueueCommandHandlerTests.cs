using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.JoinQueue;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry  = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.UnitTests.Application.Queue;

/// <summary>
/// Unit tests for JoinQueueCommandHandler.
///
/// All dependencies are Moq doubles — no database, no EF Core, no HTTP context.
/// Tests verify the handler's 7-step orchestration logic in isolation.
///
/// Key invariants exercised:
///   - "Queue not found." → DomainException (step 1)
///   - "Already in this queue." → ConflictDomainException (step 2)
///   - Queue full → QueueFullDomainException (step 3)
///   - Happy path: PositionInQueue = activeCount + 1
///   - Happy path: EstimatedWaitSeconds = position × AverageServiceTimeSeconds
///   - AddEntryAsync + SaveChangesAsync called exactly once per successful join
/// </summary>
public sealed class JoinQueueCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IQueueRepository>    _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock       = new();

    private readonly JoinQueueCommandHandler _handler;

    // ── Shared test data ──────────────────────────────────────────────────────

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrgId   = Guid.NewGuid();

    private const int MaxCapacity        = 50;
    private const int AvgServiceTimeSecs = 300;

    public JoinQueueCommandHandlerTests()
    {
        // Default: queue exists, user not already joined, capacity available, ticket = 1
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue());

        _queueRepositoryMock
            .Setup(r => r.HasActiveEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _queueRepositoryMock
            .Setup(r => r.GetActiveEntryCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _queueRepositoryMock
            .Setup(r => r.GetNextTicketNumberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _queueRepositoryMock
            .Setup(r => r.AddEntryAsync(It.IsAny<QueueEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new JoinQueueCommandHandler(
            _queueRepositoryMock.Object,
            _contextMock.Object);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsJoinQueueResult()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsTicketNumberFromRepository()
    {
        _queueRepositoryMock
            .Setup(r => r.GetNextTicketNumberAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.TicketNumber.Should().Be(42);
    }

    [Fact]
    public async Task Handle_PositionInQueue_IsActiveCountPlusOne()
    {
        // activeCount = 3 → new user joins at position 4
        _queueRepositoryMock
            .Setup(r => r.GetActiveEntryCountAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.PositionInQueue.Should().Be(4);
    }

    [Fact]
    public async Task Handle_EstimatedWaitSeconds_IsPositionTimesAvgServiceTime()
    {
        // activeCount = 4 → position = 5 → eta = 5 × 300 = 1500
        _queueRepositoryMock
            .Setup(r => r.GetActiveEntryCountAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.EstimatedWaitSeconds.Should().Be(5 * AvgServiceTimeSecs);
    }

    [Fact]
    public async Task Handle_WhenQueueEmpty_PositionIsOne_EtaEqualsAvgServiceTime()
    {
        // activeCount = 0 → position = 1 → eta = 1 × 300
        _queueRepositoryMock
            .Setup(r => r.GetActiveEntryCountAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.PositionInQueue.Should().Be(1);
        result.EstimatedWaitSeconds.Should().Be(AvgServiceTimeSecs);
    }

    [Fact]
    public async Task Handle_CallsAddEntryAsyncExactlyOnce()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _queueRepositoryMock.Verify(
            r => r.AddEntryAsync(
                It.Is<QueueEntry>(e =>
                    e.QueueId == QueueId &&
                    e.UserId  == UserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CallsSaveChangesAsyncExactlyOnce()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Queue not found (step 1) ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenQueueNotFound_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenQueueNotFound_DoesNotCallAddEntryAsync()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { /* expected */ }

        _queueRepositoryMock.Verify(
            r => r.AddEntryAsync(It.IsAny<QueueEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Already in queue (step 2) ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserAlreadyJoined_ThrowsConflictDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.HasActiveEntryAsync(QueueId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictDomainException>()
                 .WithMessage("Already in this queue.");
    }

    // ── Queue full (step 3) ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenQueueFull_ThrowsQueueFullDomainException()
    {
        // activeCount == MaxCapacity → CanAcceptEntry returns false
        _queueRepositoryMock
            .Setup(r => r.GetActiveEntryCountAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MaxCapacity);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<QueueFullDomainException>();
    }

    [Fact]
    public async Task Handle_WhenQueueFull_DoesNotCallAddEntryAsync()
    {
        _queueRepositoryMock
            .Setup(r => r.GetActiveEntryCountAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MaxCapacity);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { /* expected */ }

        _queueRepositoryMock.Verify(
            r => r.AddEntryAsync(It.IsAny<QueueEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JoinQueueCommand ValidCommand() =>
        new(QueueId: QueueId, UserId: UserId);

    private static QueueEntity BuildQueue() =>
        QueueEntity.Create(OrgId, "Main Queue", MaxCapacity, AvgServiceTimeSecs);
}
