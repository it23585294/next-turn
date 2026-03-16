using FluentAssertions;
using Moq;
using NextTurn.Application.Auth.Commands.InviteStaffUser;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class InviteStaffUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    private readonly InviteStaffUserCommandHandler _handler;
    private static readonly Guid TenantId = Guid.NewGuid();

    public InviteStaffUserCommandHandlerTests()
    {
        _tenantContextMock.Setup(t => t.TenantId).Returns(TenantId);
        _handler = new InviteStaffUserCommandHandler(
            _userRepositoryMock.Object,
            _tenantContextMock.Object,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoExistingUser_CreatesStaffUserAndSendsInvite()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new InviteStaffUserCommand("Alice Staff", "staff@example.com", "+123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.UserId.Should().NotBeEmpty();
        result.InvitePath.Should().StartWith("/staff/invite/accept?token=");
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        _userRepositoryMock.Verify(r => r.AddAsync(
            It.Is<User>(u =>
                u.TenantId == TenantId &&
                u.Role == UserRole.Staff &&
                u.Email.Value == "staff@example.com" &&
                !u.IsActive &&
                u.StaffInviteTokenHash != null),
            It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(e => e.SendStaffInviteEmailAsync(
            "staff@example.com",
            "Alice Staff",
            It.Is<string>(p => p.StartsWith("/staff/invite/accept?token=")),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingStaffUser_UpdatesAndSendsInvite()
    {
        var existing = User.Create(TenantId, "Existing", new EmailAddress("staff@example.com"), null, "hash", UserRole.Staff);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(
            new InviteStaffUserCommand("Existing", "staff@example.com", null),
            CancellationToken.None);

        result.UserId.Should().Be(existing.Id);
        _userRepositoryMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExistingUserIsNotStaff_Throws()
    {
        var existing = User.Create(TenantId, "Normal", new EmailAddress("user@example.com"), null, "hash", UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = async () => await _handler.Handle(
            new InviteStaffUserCommand("Normal", "user@example.com", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("A non-staff account already uses this email.");
        _emailServiceMock.Verify(e => e.SendStaffInviteEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}