using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.LeaveQueue;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.UnitTests.Application.Queue;

/// <summary>
/// Unit tests for LeaveQueueCommandHandler.
///
/// All dependencies are Moq doubles — no database, no EF Core, no HTTP context.
/// Tests verify the handler's 4-step orchestration logic in isolation.
///
/// Key invariants exercised:
///   - User not in queue → DomainException "You are not in this queue."
///   - Happy path: CancelEntryAsync returns true and SaveChangesAsync is called once
///   - Wrong-user guard: if repository cannot cancel for this queue/user pair, command fails
///   - SaveChangesAsync called exactly once per successful leave
/// </summary>
public sealed class LeaveQueueCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly LeaveQueueCommandHandler _handler;

    // ── Shared test data ──────────────────────────────────────────────────────

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public LeaveQueueCommandHandlerTests()
    {
        // Default: user has an active entry in the queue and cancellation succeeds
        _queueRepositoryMock
            .Setup(r => r.CancelEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new LeaveQueueCommandHandler(
            _queueRepositoryMock.Object,
            _contextMock.Object);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_Succeeds()
    {
        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_CallsCancelEntryAsyncWithCorrectParameters()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _queueRepositoryMock.Verify(
            r => r.CancelEntryAsync(QueueId, UserId, It.IsAny<CancellationToken>()),
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

    // ── User not in queue (step 1) ────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCancelEntryReturnsFalse_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.CancelEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("You are not in this queue.");
    }

    [Fact]
    public async Task Handle_WhenCancelEntryReturnsFalse_DoesNotCallSaveChangesAsync()
    {
        _queueRepositoryMock
            .Setup(r => r.CancelEntryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { /* expected */ }

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWrongUserAttemptsLeave_ThrowsDomainException()
    {
        var wrongUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var command = new LeaveQueueCommand(QueueId, wrongUserId);

        _queueRepositoryMock
            .Setup(r => r.CancelEntryAsync(QueueId, wrongUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You are not in this queue.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LeaveQueueCommand ValidCommand() =>
        new(QueueId: QueueId, UserId: UserId);
}
