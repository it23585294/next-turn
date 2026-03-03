using FluentAssertions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Queue.Commands.CreateQueue;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.UnitTests.Helpers;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using QueueEntity        = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.UnitTests.Application.Queue;

/// <summary>
/// Unit tests for <see cref="CreateQueueCommandHandler"/>.
///
/// All dependencies are Moq doubles — no database, no HTTP context.
/// Tests verify the handler's 4-step orchestration logic in isolation.
///
/// Key invariants exercised:
///   - Happy path: ShareableLink format is /queues/{tenantId}/{queueId}
///   - Happy path: QueueId embedded in ShareableLink matches the result QueueId
///   - Happy path: Queues.AddAsync + SaveChangesAsync called exactly once
///   - "Organisation not found." → DomainException when OrganisationId has no match
///   - "Wrong tenant" → DomainException when org in DB belongs to a different ID
///
/// Note on Organisation construction:
///   Organisation.Create(...) assigns a fresh Guid.NewGuid() Id internally — the
///   constructor is private, so we cannot inject a specific Id. Instead, we create
///   the test org first and capture its Id. The command then uses that captured Id,
///   so the Organisations DbSet predicate (o.Id == command.OrganisationId) resolves
///   correctly. This avoids reflection while keeping tests deterministic.
/// </summary>
public sealed class CreateQueueCommandHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IApplicationDbContext>    _contextMock     = new();
    private readonly Mock<QueueEntity>              _queueDbSetMock  = new();   // unused directly; AddAsync wired on the concrete mock below
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<QueueEntity>> _queuesDbSetMock = new();

    private readonly CreateQueueCommandHandler _handler;

    // ── Shared test data ──────────────────────────────────────────────────────

    // Org is created first so we can capture its auto-generated Id.
    private readonly OrganisationEntity _testOrg;
    private readonly Guid               _orgId;

    private const string QueueName          = "Main Counter";
    private const int    MaxCapacity        = 50;
    private const int    AvgServiceTimeSecs = 180;

    public CreateQueueCommandHandlerTests()
    {
        _testOrg = OrganisationEntity.Create(
            name:       "Test Hospital",
            address:    new Address("1 Hospital Road", "Kuala Lumpur", "50450", "Malaysia"),
            type:       OrganisationType.Healthcare,
            adminEmail: new EmailAddress("admin@testhospital.org"));

        _orgId = _testOrg.Id;

        // Default: org found — Organisations DbSet returns _testOrg
        SetupOrganisations(_testOrg);

        // Queues.AddAsync: no-op (we verify via .Verify later)
        _queuesDbSetMock
            .Setup(s => s.AddAsync(It.IsAny<QueueEntity>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<EntityEntry<QueueEntity>>(null!));

        _contextMock.Setup(c => c.Queues).Returns(_queuesDbSetMock.Object);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CreateQueueCommandHandler(_contextMock.Object);
    }

    // ── ShareableLink format ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ShareableLinkStartsWithQueuesAndOrgId()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.ShareableLink.Should().StartWith($"/queues/{_orgId}/");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShareableLinkEndsWithQueueId()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        // ShareableLink format: /queues/{tenantId}/{queueId}
        result.ShareableLink.Should().EndWith(result.QueueId.ToString());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShareableLinkMatchesExpectedTemplate()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        var expected = $"/queues/{_orgId}/{result.QueueId}";
        result.ShareableLink.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNonEmptyQueueId()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.QueueId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_CalledTwice_ReturnsDifferentQueueIds()
    {
        var result1 = await _handler.Handle(ValidCommand(), CancellationToken.None);
        var result2 = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result1.QueueId.Should().NotBe(result2.QueueId);
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_CallsQueuesAddAsyncOnce()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _queuesDbSetMock.Verify(
            s => s.AddAsync(
                It.Is<QueueEntity>(q =>
                    q.OrganisationId              == _orgId          &&
                    q.Name                        == QueueName       &&
                    q.MaxCapacity                 == MaxCapacity     &&
                    q.AverageServiceTimeSeconds   == AvgServiceTimeSecs),
                It.IsAny<CancellationToken>()),
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

    // ── Organisation not found (step 1) ──────────────────────────────────────

    [Fact]
    public async Task Handle_WhenOrgNotFound_ThrowsDomainException()
    {
        SetupOrganisations(); // empty set → FirstOrDefaultAsync returns null

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("Organisation not found.");
    }

    [Fact]
    public async Task Handle_WhenOrgNotFound_DoesNotCallQueuesAddAsync()
    {
        SetupOrganisations();

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { /* expected */ }

        _queuesDbSetMock.Verify(
            s => s.AddAsync(It.IsAny<QueueEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrgNotFound_DoesNotCallSaveChangesAsync()
    {
        SetupOrganisations();

        try { await _handler.Handle(ValidCommand(), CancellationToken.None); } catch { /* expected */ }

        _contextMock.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Wrong tenant — org exists but under a different OrganisationId ────────
    //
    // Scenario: an org admin from Tenant B sends a request carrying Tenant A's
    // OrganisationId (e.g. a crafted JWT). The controller injects the claim value
    // as the command's OrganisationId, so the DB predicate (o.Id == orgId) will
    // return null for the mismatched ID — same guard as "org not found".

    [Fact]
    public async Task Handle_WhenOrgIdBelongsToDifferentTenant_ThrowsDomainException()
    {
        // A second org exists in the DB, but its Id differs from the command's _orgId.
        var otherOrg = OrganisationEntity.Create(
            name:       "Another Agency",
            address:    new Address("2 Gov St", "Putrajaya", "62000", "Malaysia"),
            type:       OrganisationType.Government,
            adminEmail: new EmailAddress("admin@agency.gov.my"));

        // Replace the Organisations DbSet with one that only contains otherOrg.
        // The predicate (o.Id == _orgId) won't match, so FirstOrDefaultAsync → null.
        SetupOrganisations(otherOrg);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("Organisation not found.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private CreateQueueCommand ValidCommand() =>
        new(_orgId, QueueName, MaxCapacity, AvgServiceTimeSecs);

    /// <summary>
    /// Replaces the mocked Organisations DbSet with one backed by <paramref name="orgs"/>.
    /// Calling with no arguments produces an empty set, causing FirstOrDefaultAsync to return null.
    /// </summary>
    private void SetupOrganisations(params OrganisationEntity[] orgs)
    {
        var dbSetMock = AsyncQueryableHelper.BuildMockDbSet(orgs);
        _contextMock.Setup(c => c.Organisations).Returns(dbSetMock.Object);
    }
}
