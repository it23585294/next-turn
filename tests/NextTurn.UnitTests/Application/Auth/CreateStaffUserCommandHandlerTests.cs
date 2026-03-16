using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Auth.Commands.CreateStaffUser;
using NextTurn.Application.Auth.Commands.RegisterUser;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class CreateStaffUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();

    private readonly CreateStaffUserCommandHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateStaffUserCommandHandlerTests()
    {
        _tenantContextMock.Setup(t => t.TenantId).Returns(TenantId);
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _handler = new CreateStaffUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _publisherMock.Object,
            _tenantContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesStaffUserAndPublishesNotification()
    {
        var command = new CreateStaffUserCommand("Staff User", "staff@example.com", "+123", "SecureP@ss1");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _passwordHasherMock.Verify(h => h.Hash("SecureP@ss1"), Times.Once);
        _userRepositoryMock.Verify(r => r.AddAsync(
            It.Is<User>(u =>
                u.TenantId == TenantId &&
                u.Email.Value == "staff@example.com" &&
                u.Role == UserRole.Staff &&
                u.PasswordHash == "hashed-password"),
            It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<UserRegisteredNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ThrowsAndDoesNotPersist()
    {
        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateStaffUserCommand("Staff User", "staff@example.com", null, "SecureP@ss1");
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Email address is already in use.");
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}