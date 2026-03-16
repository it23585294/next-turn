using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.AssignStaffToQueue;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class AssignStaffToQueueCommandHandlerTests
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly AssignStaffToQueueCommandHandler _handler;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _queueId = Guid.NewGuid();
    private readonly Guid _staffUserId = Guid.NewGuid();

    public AssignStaffToQueueCommandHandlerTests()
    {
        _handler = new AssignStaffToQueueCommandHandler(
            _queueRepositoryMock.Object,
            _userRepositoryMock.Object,
            _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenQueueNotFound_Throws()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(_queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueEntity?)null);

        var act = async () => await _handler.Handle(new AssignStaffToQueueCommand(_queueId, _staffUserId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Queue not found.");
    }

    [Fact]
    public async Task Handle_WhenStaffUserNotFound_Throws()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByIdAsync(_queueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(QueueEntity.Create(_orgId, "Queue", 10, 60));
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _handler.Handle(new AssignStaffToQueueCommand(_queueId, _staffUserId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Staff user not found.");
    }

    [Fact]
    public async Task Handle_WhenStaffUserIsInactive_Throws()
    {
        var queue = QueueEntity.Create(_orgId, "Queue", 10, 60);
        var staff = User.Create(_orgId, "Staff", new EmailAddress("s@example.com"), null, "hash", UserRole.Staff);
        staff.Deactivate();

        _queueRepositoryMock.Setup(r => r.GetByIdAsync(_queueId, It.IsAny<CancellationToken>())).ReturnsAsync(queue);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(_staffUserId, It.IsAny<CancellationToken>())).ReturnsAsync(staff);

        var act = async () => await _handler.Handle(new AssignStaffToQueueCommand(_queueId, _staffUserId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Inactive staff accounts cannot be assigned.");
    }

    [Fact]
    public async Task Handle_WhenAlreadyAssigned_ReturnsWithoutSaving()
    {
        var queue = QueueEntity.Create(_orgId, "Queue", 10, 60);
        var staff = User.Create(_orgId, "Staff", new EmailAddress("s@example.com"), null, "hash", UserRole.Staff);

        _queueRepositoryMock.Setup(r => r.GetByIdAsync(_queueId, It.IsAny<CancellationToken>())).ReturnsAsync(queue);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(_staffUserId, It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        _queueRepositoryMock
            .Setup(r => r.IsStaffAlreadyAssignedAsync(_queueId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new AssignStaffToQueueCommand(_queueId, _staffUserId), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _queueRepositoryMock.Verify(r => r.AddStaffAssignmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidData_AddsAssignmentAndSaves()
    {
        var queue = QueueEntity.Create(_orgId, "Queue", 10, 60);
        var staff = User.Create(_orgId, "Staff", new EmailAddress("s@example.com"), null, "hash", UserRole.Staff);

        _queueRepositoryMock.Setup(r => r.GetByIdAsync(_queueId, It.IsAny<CancellationToken>())).ReturnsAsync(queue);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(_staffUserId, It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        _queueRepositoryMock
            .Setup(r => r.IsStaffAlreadyAssignedAsync(_queueId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new AssignStaffToQueueCommand(_queueId, _staffUserId), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _queueRepositoryMock.Verify(r => r.AddStaffAssignmentAsync(_orgId, _queueId, _staffUserId, It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}