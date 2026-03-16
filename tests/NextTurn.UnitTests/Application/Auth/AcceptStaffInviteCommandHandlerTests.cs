using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Moq;
using NextTurn.Application.Auth.Commands.AcceptStaffInvite;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class AcceptStaffInviteCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly AcceptStaffInviteCommandHandler _handler;

    public AcceptStaffInviteCommandHandlerTests()
    {
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("new-password-hash");
        _handler = new AcceptStaffInviteCommandHandler(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidInvite_UpdatesUserAndReturnsUnit()
    {
        const string rawToken = "raw-token-value";
        var tokenHash = ComputeSha256(rawToken);

        var staff = User.Create(Guid.NewGuid(), "Staff", new EmailAddress("staff@example.com"), null, "old-hash", UserRole.Staff);
        staff.StartStaffInvite(tokenHash, DateTimeOffset.UtcNow.AddHours(2));

        _userRepositoryMock
            .Setup(r => r.GetByStaffInviteTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var result = await _handler.Handle(new AcceptStaffInviteCommand(rawToken, "SecureP@ss1"), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        staff.IsActive.Should().BeTrue();
        staff.StaffInviteTokenHash.Should().BeNull();
        staff.PasswordHash.Should().Be("new-password-hash");
        _passwordHasherMock.Verify(h => h.Hash("SecureP@ss1"), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateAsync(staff, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenDoesNotMatch_Throws()
    {
        _userRepositoryMock
            .Setup(r => r.GetByStaffInviteTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _handler.Handle(new AcceptStaffInviteCommand("bad-token", "SecureP@ss1"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invite token is invalid or expired.");
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}