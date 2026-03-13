using FluentAssertions;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Entities;
using NextTurn.Domain.Queue.Enums;

namespace NextTurn.UnitTests.Domain.Queue;

/// <summary>
/// Unit tests for the QueueEntry child entity.
/// Covers factory method validation and property initialisation.
/// </summary>
public sealed class QueueEntryTests
{
    // ── Shared valid inputs ───────────────────────────────────────────────────

    private static readonly Guid ValidQueueId = Guid.NewGuid();
    private static readonly Guid ValidUserId  = Guid.NewGuid();
    private const int ValidTicketNumber = 1;

    // ── QueueEntry.Create happy path ──────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_SetsAllProperties()
    {
        var entry = QueueEntry.Create(ValidQueueId, ValidUserId, ticketNumber: 42);

        entry.QueueId.Should().Be(ValidQueueId);
        entry.UserId.Should().Be(ValidUserId);
        entry.TicketNumber.Should().Be(42);
    }

    [Fact]
    public void Create_SetsStatusToWaiting()
    {
        var entry = QueueEntry.Create(ValidQueueId, ValidUserId, ValidTicketNumber);

        entry.Status.Should().Be(QueueEntryStatus.Waiting);
    }

    [Fact]
    public void Create_GeneratesNonEmptyId()
    {
        var entry = QueueEntry.Create(ValidQueueId, ValidUserId, ValidTicketNumber);

        entry.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var e1 = QueueEntry.Create(ValidQueueId, ValidUserId, ticketNumber: 1);
        var e2 = QueueEntry.Create(ValidQueueId, ValidUserId, ticketNumber: 2);

        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void Create_SetsJoinedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var entry  = QueueEntry.Create(ValidQueueId, ValidUserId, ValidTicketNumber);
        var after  = DateTimeOffset.UtcNow;

        entry.JoinedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    // ── QueueEntry.Create validation failures ─────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Create_WithTicketNumberLessThanOne_ThrowsArgumentOutOfRangeException(int ticketNumber)
    {
        var act = () => QueueEntry.Create(ValidQueueId, ValidUserId, ticketNumber);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("ticketNumber")
           .WithMessage("*Ticket number must be at least 1*");
    }

    [Fact]
    public void Create_WithTicketNumberOne_Succeeds()
    {
        var act = () => QueueEntry.Create(ValidQueueId, ValidUserId, ticketNumber: 1);

        act.Should().NotThrow();
    }

    // ── QueueEntry.Cancel behaviour ──────────────────────────────────────────

    [Fact]
    public void Cancel_WhenEntryIsWaiting_SetsStatusToCancelled()
    {
        var entry = QueueEntry.Create(ValidQueueId, ValidUserId, ValidTicketNumber);

        entry.Cancel();

        entry.Status.Should().Be(QueueEntryStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenEntryAlreadyCancelled_ThrowsDomainException()
    {
        var entry = QueueEntry.Create(ValidQueueId, ValidUserId, ValidTicketNumber);
        entry.Cancel();

        var act = () => entry.Cancel();

        act.Should().Throw<DomainException>()
           .WithMessage("Queue entry is already in a terminal state and cannot be cancelled.");
    }

    [Theory]
    [InlineData(QueueEntryStatus.Served)]
    [InlineData(QueueEntryStatus.NoShow)]
    public void Cancel_WhenEntryInOtherTerminalState_ThrowsDomainException(QueueEntryStatus terminalStatus)
    {
        var entry = QueueEntry.Create(ValidQueueId, ValidUserId, ValidTicketNumber);
        SetStatus(entry, terminalStatus);

        var act = () => entry.Cancel();

        act.Should().Throw<DomainException>()
           .WithMessage("Queue entry is already in a terminal state and cannot be cancelled.");
    }

    private static void SetStatus(QueueEntry entry, QueueEntryStatus status)
    {
        typeof(QueueEntry)
            .GetProperty(nameof(QueueEntry.Status))!
            .SetValue(entry, status);
    }
}
