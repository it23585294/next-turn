using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.CallNext;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class CallNextCommandHandlerTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly CallNextCommandHandler _handler;

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public CallNextCommandHandlerTests()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue());

        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        _queueRepositoryMock
            .Setup(r => r.GetNextWaitingEntryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueueEntry.Create(QueueId, UserId, 3));

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CallNextCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithWaitingEntry_MarksTicketAsServing()
    {
        var result = await _handler.Handle(new CallNextCommand(QueueId), CancellationToken.None);

        result.TicketNumber.Should().Be(3);
        result.Status.Should().Be("Serving");

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQueueNotFound_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var act = async () => await _handler.Handle(new CallNextCommand(QueueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenAlreadyServingEntryExists_ThrowsConflictDomainException()
    {
        var serving = QueueEntry.Create(QueueId, UserId, 1);
        serving.StartServing();

        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serving);

        var act = async () => await _handler.Handle(new CallNextCommand(QueueId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictDomainException>()
            .WithMessage("A ticket is already being served.");
    }

    [Fact]
    public async Task Handle_WhenNoWaitingEntry_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetNextWaitingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        var act = async () => await _handler.Handle(new CallNextCommand(QueueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("No waiting entries in this queue.");
    }

    private static QueueEntity BuildQueue() =>
        QueueEntity.Create(OrgId, "Main Queue", maxCapacity: 50, averageServiceTimeSeconds: 180);
}
