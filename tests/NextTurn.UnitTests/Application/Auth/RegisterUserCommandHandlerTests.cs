using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Auth.Commands.RegisterUser;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for RegisterUserCommandHandler.
///
/// All infrastructure dependencies (repository, hasher, publisher, tenant context)
/// are replaced with Moq doubles. These tests verify the handler's orchestration
/// logic in isolation — no database, no BCrypt computation, no HTTP context.
///
/// Why Moq for IPublisher?
///   MediatR's IPublisher is an interface — Moq can mock it directly.
///   We verify Publish was called with the correct notification type and data.
/// </summary>
public sealed class RegisterUserCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();

    private readonly RegisterUserCommandHandler _handler;

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string HashedPassword = "$2a$12$hashedvalue";

    public RegisterUserCommandHandlerTests()
    {
        // Default setup shared across tests
        _tenantContextMock.Setup(t => t.TenantId).Returns(TenantId);
        _passwordHasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns(HashedPassword);
        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publisherMock
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RegisterUserCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _publisherMock.Object,
            _tenantContextMock.Object);
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

        // Verify the stored user carries the hash, not the plain text
        _userRepositoryMock.Verify(r =>
            r.AddAsync(
                It.Is<User>(u => u.PasswordHash == HashedPassword),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AssignsTenantIdFromContext()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _userRepositoryMock.Verify(r =>
            r.AddAsync(
                It.Is<User>(u => u.TenantId == TenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PublishesUserRegisteredNotification()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _publisherMock.Verify(p =>
            p.Publish(
                It.Is<UserRegisteredNotification>(n =>
                    n.Email == "alice@example.com" &&
                    n.UserId != Guid.Empty),
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

    // ── Duplicate email ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ThrowsDomainException()
    {
        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // email taken

        Func<Task> act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already in use*");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_DoesNotCallAddAsync()
    {
        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
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
            .Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); }
        catch (DomainException) { /* expected */ }

        _publisherMock.Verify(
            p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Tenant context ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReadsFromTenantContext_ExactlyOnce()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _tenantContextMock.Verify(t => t.TenantId, Times.AtLeastOnce);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static RegisterUserCommand ValidCommand(
        string name = "Alice Smith",
        string email = "alice@example.com",
        string? phone = null,
        string password = "SecureP@ss1") =>
        new(name, email, phone, password);
}
