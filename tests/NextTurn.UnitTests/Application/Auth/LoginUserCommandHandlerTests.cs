using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Auth.Commands.LoginUser;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for LoginUserCommandHandler.
///
/// All dependencies are Moq doubles — no database, no BCrypt, no JWT library.
/// Tests verify the handler's 7-step orchestration logic in isolation.
///
/// Key design verified:
///   - "Invalid credentials." is thrown for BOTH non-existent email AND wrong password.
///     These scenarios must produce identical error messages to prevent user enumeration.
///   - Lockout is checked BEFORE password verification so locked users can't probe passwords.
///   - UpdateAsync is called on every failed attempt so lockout is durable.
///   - AccountLockedNotification is published exactly when the 3rd failure triggers lockout.
/// </summary>
public sealed class LoginUserCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IUserRepository>    _userRepositoryMock    = new();
    private readonly Mock<IPasswordHasher>    _passwordHasherMock    = new();
    private readonly Mock<IJwtTokenService>   _jwtTokenServiceMock   = new();
    private readonly Mock<IPublisher>         _publisherMock         = new();
    private readonly Mock<ITenantContext>     _tenantContextMock     = new();

    private readonly LoginUserCommandHandler _handler;

    private static readonly Guid   TenantId     = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly string FakeToken    = "header.payload.signature";
    private static readonly string ValidEmail   = "alice@example.com";
    private static readonly string ValidPassword = "SecureP@ss1";

    public LoginUserCommandHandlerTests()
    {
        _tenantContextMock.Setup(t => t.TenantId).Returns(TenantId);
        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<User>(), It.IsAny<Guid>()))
            .Returns(FakeToken);
        _publisherMock
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Default: password is correct
        _passwordHasherMock
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Default: user exists and is not locked
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUser());
        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new LoginUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _publisherMock.Object,
            _tenantContextMock.Object);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsLoginResult()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be(FakeToken);
        result.Email.Should().Be(ValidEmail);
        result.Role.Should().Be(UserRole.User.ToString());
    }

    [Fact]
    public async Task Handle_WithValidCredentials_CallsGenerateToken()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _jwtTokenServiceMock.Verify(
            j => j.GenerateToken(It.IsAny<User>(), TenantId),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_CallsUpdateAsyncToResetFailedAttempts()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        // Unlock() is called on success, then UpdateAsync persists the reset counter
        _userRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_DoesNotPublishAnyNotification()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Non-existent user ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsDomainExceptionWithGenericMessage()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_DoesNotCallGenerateToken()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _jwtTokenServiceMock.Verify(
            j => j.GenerateToken(It.IsAny<User>(), It.IsAny<Guid>()),
            Times.Never);
    }

    // ── Wrong password ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_ThrowsDomainExceptionWithGenericMessage()
    {
        // Same message as "not found" — intentional, prevents user enumeration
        _passwordHasherMock
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        Func<Task> act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_CallsUpdateAsyncToRecordFailedAttempt()
    {
        _passwordHasherMock
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _userRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_DoesNotCallGenerateToken()
    {
        _passwordHasherMock
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _jwtTokenServiceMock.Verify(
            j => j.GenerateToken(It.IsAny<User>(), It.IsAny<Guid>()),
            Times.Never);
    }

    // ── Account already locked ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAccountIsLocked_ThrowsDomainExceptionWithLockoutMessage()
    {
        // Build a user that is already locked
        var lockedUser = BuildUser();
        lockedUser.Lock(TimeSpan.FromMinutes(10));

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockedUser);

        Func<Task> act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*temporarily locked*");
    }

    [Fact]
    public async Task Handle_WhenAccountIsLocked_DoesNotVerifyPassword()
    {
        // Lockout is checked BEFORE password verification — locked users are rejected early
        var lockedUser = BuildUser();
        lockedUser.Lock(TimeSpan.FromMinutes(10));

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockedUser);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _passwordHasherMock.Verify(
            h => h.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── Third failure triggers lockout ────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenThirdFailedAttempt_PublishesAccountLockedNotification()
    {
        // User already has 2 failures — the next wrong password triggers lockout
        var userWithTwoFailures = BuildUser();
        userWithTwoFailures.RecordFailedLogin(); // 1
        userWithTwoFailures.RecordFailedLogin(); // 2 — still under threshold

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userWithTwoFailures);

        _passwordHasherMock
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false); // 3rd wrong attempt

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _publisherMock.Verify(p =>
            p.Publish(
                It.Is<AccountLockedNotification>(n =>
                    n.Email == ValidEmail &&
                    n.UserId != Guid.Empty &&
                    n.LockoutUntil > DateTimeOffset.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFirstOrSecondFailedAttempt_DoesNotPublishLockoutNotification()
    {
        // Only 1 prior failure — threshold not reached after this attempt
        var userWithOneFailure = BuildUser();
        userWithOneFailure.RecordFailedLogin(); // 1

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userWithOneFailure);

        _passwordHasherMock
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false); // 2nd wrong attempt

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<AccountLockedNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LoginUserCommand ValidCommand(
        string email    = "alice@example.com",
        string password = "SecureP@ss1") => new(email, password);

    private static User BuildUser() =>
        User.Create(
            tenantId:     Guid.Parse("22222222-2222-2222-2222-222222222222"),
            name:         "Alice Smith",
            email:        new EmailAddress("alice@example.com"),
            phone:        null,
            passwordHash: "$2a$12$hashedvalue");
}
