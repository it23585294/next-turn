using FluentAssertions;
using Moq;
using NextTurn.Application.Queue.Queries.ListOrgQueues;
using NextTurn.Domain.Queue.Repositories;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.UnitTests.Application.Queue;

/// <summary>
/// Unit tests for ListOrgQueuesQueryHandler.
///
/// All dependencies are Moq doubles — no database, no HttpContext.
/// Tests verify the handler's projection logic in isolation:
///   - Queue fields are mapped to OrgQueueSummary properties correctly.
///   - ShareableLink is built as /queues/{organisationId}/{queueId}.
///   - Empty repository response returns an empty list (not an error).
///   - OrganisationId is forwarded to the repository correctly.
/// </summary>
public sealed class ListOrgQueuesQueryHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();

    private readonly ListOrgQueuesQueryHandler _handler;

    // ── Shared test data ──────────────────────────────────────────────────────

    private static readonly Guid OrganisationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ListOrgQueuesQueryHandlerTests()
    {
        // Default: one queue exists for the organisation
        _queueRepositoryMock
            .Setup(r => r.GetByOrganisationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueEntity>
            {
                BuildQueue("Support Queue", 50, 300)
            });

        _handler = new ListOrgQueuesQueryHandler(_queueRepositoryMock.Object);
    }

    // ── Happy path — single queue ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithOneQueue_ReturnsOneOrgQueueSummary()
    {
        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithOneQueue_PropagatesName()
    {
        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result[0].Name.Should().Be("Support Queue");
    }

    [Fact]
    public async Task Handle_WithOneQueue_PropagatesMaxCapacity()
    {
        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result[0].MaxCapacity.Should().Be(50);
    }

    [Fact]
    public async Task Handle_WithOneQueue_PropagatesAverageServiceTimeSeconds()
    {
        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result[0].AverageServiceTimeSeconds.Should().Be(300);
    }

    [Fact]
    public async Task Handle_WithOneQueue_PropagatesStatus()
    {
        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result[0].Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_WithOneQueue_BuildsShareableLinkContainingOrganisationId()
    {
        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result[0].ShareableLink.Should().Contain(OrganisationId.ToString());
    }

    [Fact]
    public async Task Handle_WithOneQueue_BuildsShareableLinkContainingQueueId()
    {
        var queue = BuildQueue("Service Desk", 20, 120);

        _queueRepositoryMock
            .Setup(r => r.GetByOrganisationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueEntity> { queue });

        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result[0].ShareableLink.Should().Be($"/queues/{OrganisationId}/{queue.Id}");
    }

    // ── Multiple queues ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithMultipleQueues_ReturnsAllMapped()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByOrganisationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueEntity>
            {
                BuildQueue("Queue A", 10, 60),
                BuildQueue("Queue B", 20, 120),
                BuildQueue("Queue C", 30, 180)
            });

        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().BeEquivalentTo("Queue A", "Queue B", "Queue C");
    }

    // ── Empty result ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoQueues_ReturnsEmptyList()
    {
        _queueRepositoryMock
            .Setup(r => r.GetByOrganisationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueueEntity>());

        var result = await _handler.Handle(new ListOrgQueuesQuery(OrganisationId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── OrganisationId forwarding ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ForwardsOrganisationIdToRepository()
    {
        var specificOrgId = Guid.NewGuid();

        await _handler.Handle(new ListOrgQueuesQuery(specificOrgId), CancellationToken.None);

        _queueRepositoryMock.Verify(
            r => r.GetByOrganisationIdAsync(specificOrgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static QueueEntity BuildQueue(
        string name                      = "Test Queue",
        int    maxCapacity               = 50,
        int    averageServiceTimeSeconds = 300) =>
        QueueEntity.Create(OrganisationId, name, maxCapacity, averageServiceTimeSeconds);
}
