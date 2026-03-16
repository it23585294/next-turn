using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.MarkNoShow;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class MarkNoShowCommandHandlerTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly MarkNoShowCommandHandler _handler;

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public MarkNoShowCommandHandlerTests()
    {
        var serving = QueueEntry.Create(QueueId, UserId, 4);
        serving.StartServing();

        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue());

        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serving);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new MarkNoShowCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithServingEntry_MarksAsNoShow()
    {
        var result = await _handler.Handle(new MarkNoShowCommand(QueueId), CancellationToken.None);

        result.Status.Should().Be("NoShow");
        result.TicketNumber.Should().Be(4);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQueueNotFound_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var act = async () => await _handler.Handle(new MarkNoShowCommand(QueueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenNoServingEntry_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        var act = async () => await _handler.Handle(new MarkNoShowCommand(QueueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("No entry is currently being served.");
    }

    private static QueueEntity BuildQueue() =>
        QueueEntity.Create(OrgId, "Main Queue", maxCapacity: 50, averageServiceTimeSeconds: 180);
}
