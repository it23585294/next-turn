using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Auth.Commands.LoginGlobalUser;
using NextTurn.Application.Auth.Commands.LoginUser;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for LoginGlobalUserCommandHandler.
///
/// Key design verified:
///   - Uses GetByEmailGlobalAsync (cross-tenant lookup), NOT GetByEmailAsync.
///   - No ITenantContext — global users are not bound to any org.
///   - JWT is generated with TenantId = Guid.Empty (consumer accounts have no tenant).
///   - "Invalid credentials." is thrown for BOTH non-existent email AND wrong password
///     (same message prevents user enumeration).
///   - Lockout is checked BEFORE password verification.
///   - UpdateAsync is called on every failed attempt (persists lockout state).
///   - AccountLockedNotification is published exactly when the 3rd failure triggers lockout.
/// </summary>
public sealed class LoginGlobalUserCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IUserRepository>  _userRepositoryMock  = new();
    private readonly Mock<IPasswordHasher>  _passwordHasherMock  = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IPublisher>       _publisherMock       = new();

    // NOTE: No ITenantContext — global handler does not inject it.

    private readonly LoginGlobalUserCommandHandler _handler;

    private static readonly string FakeToken     = "header.payload.signature";
    private static readonly string ValidEmail    = "alice@example.com";
    private static readonly string ValidPassword = "SecureP@ss1";

    public LoginGlobalUserCommandHandlerTests()
    {
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

        // Default: global user exists and is not locked
        _userRepositoryMock
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUser());

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new LoginGlobalUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _publisherMock.Object);
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
    public async Task Handle_WithValidCredentials_CallsGenerateTokenWithGuidEmpty()
    {
        // Global users have TenantId = Guid.Empty; JWT tid claim must reflect this.
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _jwtTokenServiceMock.Verify(
            j => j.GenerateToken(It.IsAny<User>(), Guid.Empty),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_UsesGlobalEmailLookup()
    {
        // Must call GetByEmailGlobalAsync, NOT the tenant-scoped GetByEmailAsync.
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _userRepositoryMock.Verify(
            r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_CallsUpdateAsyncToResetFailedAttempts()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

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
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_DoesNotCallGenerateToken()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
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
        var lockedUser = BuildUser();
        lockedUser.Lock(TimeSpan.FromMinutes(10));

        _userRepositoryMock
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockedUser);

        Func<Task> act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*temporarily locked*");
    }

    [Fact]
    public async Task Handle_WhenAccountIsLocked_DoesNotVerifyPassword()
    {
        // Lockout is checked BEFORE password verification.
        var lockedUser = BuildUser();
        lockedUser.Lock(TimeSpan.FromMinutes(10));

        _userRepositoryMock
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
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
        var userWithTwoFailures = BuildUser();
        userWithTwoFailures.RecordFailedLogin(); // 1
        userWithTwoFailures.RecordFailedLogin(); // 2

        _userRepositoryMock
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
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
        var userWithOneFailure = BuildUser();
        userWithOneFailure.RecordFailedLogin(); // 1

        _userRepositoryMock
            .Setup(r => r.GetByEmailGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
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

    private static LoginGlobalUserCommand ValidCommand(
        string email    = "alice@example.com",
        string password = "SecureP@ss1") => new(email, password);

    private static User BuildUser() =>
        User.Create(
            tenantId:     Guid.Empty,        // consumer users have no tenant
            name:         "Alice Smith",
            email:        new EmailAddress("alice@example.com"),
            phone:        null,
            passwordHash: "$2a$12$hashedvalue");
}
