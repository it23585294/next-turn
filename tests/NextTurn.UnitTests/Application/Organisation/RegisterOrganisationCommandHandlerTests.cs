using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Organisation.Commands.RegisterOrganisation;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Repositories;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Application.Organisation;

/// <summary>
/// Unit tests for RegisterOrganisationCommandHandler.
///
/// All infrastructure dependencies are replaced with Moq doubles.
/// Tests verify orchestration logic: registry check, uniqueness guard,
/// domain object construction, persistence, email dispatch, and result shape.
/// </summary>
public sealed class RegisterOrganisationCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IOrganisationRepository>  _orgRepoMock     = new();
    private readonly Mock<IApplicationDbContext>    _contextMock     = new();
    private readonly Mock<IPasswordHasher>          _hasherMock      = new();
    private readonly Mock<IEmailService>            _emailMock       = new();
    private readonly Mock<IBusinessRegistryService> _registryMock    = new();
    private readonly Mock<DbSet<User>>              _userDbSetMock   = new();

    private readonly RegisterOrganisationCommandHandler _handler;

    private const string HashedPassword = "$2a$12$unittesthashedvalue";

    public RegisterOrganisationCommandHandlerTests()
    {
        // Business registry always passes by default
        _registryMock
            .Setup(b => b.IsRegisteredBusinessAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // No existing org by default
        _orgRepoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganisationEntity?)null);

        _orgRepoMock
            .Setup(r => r.AddAsync(It.IsAny<OrganisationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Password hasher returns deterministic hash
        _hasherMock
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns(HashedPassword);

        // Email service is a no-op by default
        _emailMock
            .Setup(e => e.SendWelcomeEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // DbSet and context setup
        _userDbSetMock
            .Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<EntityEntry<User>>(null!));

        _contextMock.Setup(c => c.Users).Returns(_userDbSetMock.Object);
        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _handler = new RegisterOrganisationCommandHandler(
            _orgRepoMock.Object,
            _contextMock.Object,
            _hasherMock.Object,
            _emailMock.Object,
            _registryMock.Object);
    }

    // ── Result shape ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNonEmptyOrganisationId()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.OrganisationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNonEmptyAdminUserId()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.AdminUserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_CalledTwice_ReturnsDifferentOrganisationIds()
    {
        var result1 = await _handler.Handle(ValidCommand("Acme"), CancellationToken.None);
        var result2 = await _handler.Handle(ValidCommand("Beta"), CancellationToken.None);

        result1.OrganisationId.Should().NotBe(result2.OrganisationId);
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CallsAddAsyncOnOrganisationRepository()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _orgRepoMock.Verify(
            r => r.AddAsync(It.IsAny<OrganisationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsAddAsyncOnUserDbSet()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _userDbSetMock.Verify(
            s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CallsSaveChangesAsyncOnce()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── OrgAdmin user properties ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CreatesAdminUserWithOrgAdminRole()
    {
        User? capturedUser = null;
        _userDbSetMock
            .Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(ValueTask.FromResult<EntityEntry<User>>(null!));

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser.Should().NotBeNull();
        capturedUser!.Role.Should().Be(UserRole.OrgAdmin);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsAdminUserTenantIdToOrganisationId()
    {
        User? capturedUser = null;
        _userDbSetMock
            .Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(ValueTask.FromResult<EntityEntry<User>>(null!));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser!.TenantId.Should().Be(result.OrganisationId);
    }

    [Fact]
    public async Task Handle_WithValidCommand_StoresHashedPasswordNotPlainText()
    {
        User? capturedUser = null;
        _userDbSetMock
            .Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(ValueTask.FromResult<EntityEntry<User>>(null!));

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser!.PasswordHash.Should().Be(HashedPassword);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SetsAdminEmailOnUser()
    {
        const string adminEmail = "admin@acme.com";
        User? capturedUser = null;
        _userDbSetMock
            .Setup(s => s.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(ValueTask.FromResult<EntityEntry<User>>(null!));

        await _handler.Handle(ValidCommand(adminEmail: adminEmail), CancellationToken.None);

        capturedUser!.Email.Value.Should().Be(adminEmail);
    }

    // ── Password hashing ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CallsHasherWithGeneratedPassword()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _hasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Once);
    }

    // ── Business registry ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ChecksRegistryWithCorrectOrgNameAndCountry()
    {
        const string orgName = "Acme Corp";
        const string country = "UK";

        await _handler.Handle(ValidCommand(orgName: orgName, country: country), CancellationToken.None);

        _registryMock.Verify(b =>
            b.IsRegisteredBusinessAsync(orgName, country, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBusinessRegistryReturnsFalse_ThrowsDomainException()
    {
        _registryMock
            .Setup(b => b.IsRegisteredBusinessAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*business registry*");
    }

    [Fact]
    public async Task Handle_WhenBusinessRegistryReturnsFalse_DoesNotCallSaveChanges()
    {
        _registryMock
            .Setup(b => b.IsRegisteredBusinessAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { }

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Duplicate name guard ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenOrgNameAlreadyExists_ThrowsConflictDomainException()
    {
        _orgRepoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStubOrganisation());

        var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictDomainException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task Handle_WhenOrgNameAlreadyExists_DoesNotCallSaveChanges()
    {
        _orgRepoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStubOrganisation());

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { }

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Email dispatch ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_SendsWelcomeEmailToAdminEmail()
    {
        const string adminEmail = "admin@acme.com";

        await _handler.Handle(ValidCommand(adminEmail: adminEmail), CancellationToken.None);

        _emailMock.Verify(e =>
            e.SendWelcomeEmailAsync(
                adminEmail,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SendsWelcomeEmailWithOrgName()
    {
        const string orgName = "Acme Corp";

        await _handler.Handle(ValidCommand(orgName: orgName), CancellationToken.None);

        _emailMock.Verify(e =>
            e.SendWelcomeEmailAsync(
                It.IsAny<string>(),
                orgName,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_SendsWelcomeEmailWithNonEmptyTemporaryPassword()
    {
        string? capturedPassword = null;
        _emailMock
            .Setup(e => e.SendWelcomeEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, pw, _) => capturedPassword = pw)
            .Returns(Task.CompletedTask);

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedPassword.Should().NotBeNullOrWhiteSpace();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RegisterOrganisationCommand ValidCommand(
        string orgName    = "Acme Corp",
        string country    = "UK",
        string adminEmail = "admin@acme.com") =>
        new(
            OrgName:      orgName,
            AddressLine1: "123 Main St",
            City:         "London",
            PostalCode:   "SW1A 1AA",
            Country:      country,
            OrgType:      "Healthcare",
            AdminName:    "Jane Smith",
            AdminEmail:   adminEmail);

    private static OrganisationEntity CreateStubOrganisation() =>
        OrganisationEntity.Create(
            "Existing Corp",
            new NextTurn.Domain.Organisation.ValueObjects.Address("1 Old St", "London", "EC1A 1BB", "UK"),
            NextTurn.Domain.Organisation.Enums.OrganisationType.Healthcare,
            new NextTurn.Domain.Auth.ValueObjects.EmailAddress("existing@example.com"));
}
