using FluentAssertions;
using Moq;
using NextTurn.Application.Queue.Queries.GetQueueDashboard;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Enums;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class GetQueueDashboardQueryHandlerTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly GetQueueDashboardQueryHandler _handler;

    private static readonly Guid QueueId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserAId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserBId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid OrgId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public GetQueueDashboardQueryHandlerTests()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildQueue());

        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntry?)null);

        _queueRepositoryMock
            .Setup(r => r.GetWaitingEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueEntry>
            {
                QueueEntry.Create(QueueId, UserAId, 1),
                QueueEntry.Create(QueueId, UserBId, 2),
            });

        _handler = new GetQueueDashboardQueryHandler(_queueRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQueue_ReturnsDashboardData()
    {
        var result = await _handler.Handle(new GetQueueDashboardQuery(QueueId), CancellationToken.None);

        result.QueueId.Should().Be(QueueId);
        result.QueueName.Should().Be("Main Queue");
        result.QueueStatus.Should().Be(QueueStatus.Active.ToString());
        result.WaitingCount.Should().Be(2);
        result.CurrentlyServing.Should().BeNull();
        result.WaitingEntries.Select(x => x.TicketNumber).Should().ContainInOrder(1, 2);
    }

    [Fact]
    public async Task Handle_WhenServingEntryExists_MapsCurrentServing()
    {
        var serving = QueueEntry.Create(QueueId, UserAId, 7);
        serving.StartServing();

        _queueRepositoryMock
            .Setup(r => r.GetCurrentServingEntryAsync(QueueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serving);

        var result = await _handler.Handle(new GetQueueDashboardQuery(QueueId), CancellationToken.None);

        result.CurrentlyServing.Should().NotBeNull();
        result.CurrentlyServing!.TicketNumber.Should().Be(7);
        result.CurrentlyServing.EntryId.Should().Be(serving.Id);
    }

    [Fact]
    public async Task Handle_WhenQueueNotFound_ThrowsDomainException()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var act = async () => await _handler.Handle(new GetQueueDashboardQuery(QueueId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Queue not found.");
    }

    private static QueueEntity BuildQueue() =>
        QueueEntity.Create(OrgId, "Main Queue", maxCapacity: 50, averageServiceTimeSeconds: 180);
}
