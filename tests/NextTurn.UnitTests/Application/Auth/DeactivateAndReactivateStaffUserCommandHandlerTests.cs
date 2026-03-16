using FluentAssertions;
using Moq;
using NextTurn.Application.Auth.Commands.DeactivateStaffUser;
using NextTurn.Application.Auth.Commands.ReactivateStaffUser;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class DeactivateAndReactivateStaffUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();

    [Fact]
    public async Task Deactivate_WhenUserNotFound_Throws()
    {
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var handler = new DeactivateStaffUserCommandHandler(_userRepositoryMock.Object);

        var act = async () => await handler.Handle(new DeactivateStaffUserCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Staff user not found.");
    }

    [Fact]
    public async Task Deactivate_WhenAlreadyInactive_ReturnsWithoutUpdate()
    {
        var user = User.Create(Guid.NewGuid(), "Staff", new EmailAddress("s@example.com"), null, "hash", UserRole.Staff);
        user.Deactivate();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new DeactivateStaffUserCommandHandler(_userRepositoryMock.Object);

        var result = await handler.Handle(new DeactivateStaffUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reactivate_WhenInactiveStaff_UpdatesUser()
    {
        var user = User.Create(Guid.NewGuid(), "Staff", new EmailAddress("s@example.com"), null, "hash", UserRole.Staff);
        user.Deactivate();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new ReactivateStaffUserCommandHandler(_userRepositoryMock.Object);

        var result = await handler.Handle(new ReactivateStaffUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        user.IsActive.Should().BeTrue();
        _userRepositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reactivate_WhenUserIsNotStaff_Throws()
    {
        var user = User.Create(Guid.NewGuid(), "User", new EmailAddress("u@example.com"), null, "hash", UserRole.User);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new ReactivateStaffUserCommandHandler(_userRepositoryMock.Object);

        var act = async () => await handler.Handle(new ReactivateStaffUserCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Only staff accounts can be reactivated from this endpoint.");
    }
}