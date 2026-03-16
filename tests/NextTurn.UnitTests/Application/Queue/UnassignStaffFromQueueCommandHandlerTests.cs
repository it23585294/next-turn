using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.UnassignStaffFromQueue;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class UnassignStaffFromQueueCommandHandlerTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WhenQueueNotFound_Throws()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var handler = new UnassignStaffFromQueueCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);
        var act = async () => await handler.Handle(new UnassignStaffFromQueueCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WithValidRequest_RemovesAssignmentAndSaves()
    {
        var orgId = Guid.NewGuid();
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueueEntity.Create(orgId, "Queue", 10, 60));

        var queueId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        var handler = new UnassignStaffFromQueueCommandHandler(_queueRepositoryMock.Object, _contextMock.Object);
        var result = await handler.Handle(new UnassignStaffFromQueueCommand(queueId, staffId), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _queueRepositoryMock.Verify(r => r.RemoveStaffAssignmentAsync(queueId, staffId, It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}