using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Auth.Commands.RegisterGlobalUser;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for RegisterGlobalUserCommandHandler.
///
/// Key design verified:
///   - Uses ExistsGlobalAsync (cross-tenant uniqueness), NOT ExistsAsync.
///   - Created user always has TenantId == Guid.Empty (no-tenant consumer).
///   - On duplicate email: no AddAsync, no notification.
///   - Password is hashed before persisting; plain text never stored.
///   - RegisterGlobalUserNotification is published with the correct UserId + Email.
/// </summary>
public sealed class RegisterGlobalUserCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IPublisher>      _publisherMock      = new();

    // NOTE: No ITenantContext — global handler does not inject it.

    private readonly RegisterGlobalUserCommandHandler _handler;

    private const string HashedPassword = "$2a$12$hashedvalue";

    public RegisterGlobalUserCommandHandlerTests()
    {
        _passwordHasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns(HashedPassword);

        _userRepositoryMock
            .Setup(r => r.ExistsGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // email available by default

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _publisherMock
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RegisterGlobalUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _publisherMock.Object);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CompletesSuccessfully()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsAddAsyncOnRepository()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_HashesPasswordBeforePersisting()
    {
        const string plainPassword = "SecureP@ss1";
        await _handler.Handle(ValidCommand(password: plainPassword), CancellationToken.None);

        _passwordHasherMock.Verify(h => h.Hash(plainPassword), Times.Once);

        _userRepositoryMock.Verify(r =>
            r.AddAsync(
                It.Is<User>(u => u.PasswordHash == HashedPassword),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreateUserWithGuidEmptyTenantId()
    {
        // Consumer users must NOT be bound to any tenant.
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _userRepositoryMock.Verify(r =>
            r.AddAsync(
                It.Is<User>(u => u.TenantId == Guid.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsCorrectEmailOnCreatedUser()
    {
        await _handler.Handle(ValidCommand(email: "alice@example.com"), CancellationToken.None);

        _userRepositoryMock.Verify(r =>
            r.AddAsync(
                It.Is<User>(u => u.Email.Value == "alice@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesRegisterGlobalUserNotification()
    {
        await _handler.Handle(ValidCommand(email: "alice@example.com"), CancellationToken.None);

        _publisherMock.Verify(p =>
            p.Publish(
                It.Is<RegisterGlobalUserNotification>(n =>
                    n.Email == "alice@example.com" &&
                    n.UserId != Guid.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ChecksGlobalEmailUniqueness()
    {
        // Must call ExistsGlobalAsync, NOT the tenant-scoped ExistsAsync.
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _userRepositoryMock.Verify(
            r => r.ExistsGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Duplicate email ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ThrowsDomainException()
    {
        _userRepositoryMock
            .Setup(r => r.ExistsGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already in use*");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_DoesNotCallAddAsync()
    {
        _userRepositoryMock
            .Setup(r => r.ExistsGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _userRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_DoesNotPublishNotification()
    {
        _userRepositoryMock
            .Setup(r => r.ExistsGlobalAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static RegisterGlobalUserCommand ValidCommand(
        string  name     = "Alice Smith",
        string  email    = "alice@example.com",
        string? phone    = null,
        string  password = "SecureP@ss1") =>
        new(name, email, phone, password);
}
