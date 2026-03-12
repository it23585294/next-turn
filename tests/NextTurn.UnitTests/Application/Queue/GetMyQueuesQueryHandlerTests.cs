using FluentAssertions;
using Moq;
using NextTurn.Application.Queue.Queries.GetMyQueues;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.UnitTests.Application.Queue;

/// <summary>
/// Unit tests for GetMyQueuesQueryHandler.
///
/// All dependencies are Moq doubles — no database, no HttpContext.
/// Tests verify the handler's projection logic in isolation:
///   - Active entries are mapped to MyQueueEntry with all fields propagated.
///   - Empty repository response returns an empty list (not an error).
///   - UserId is forwarded to the repository correctly.
/// </summary>
public sealed class GetMyQueuesQueryHandlerTests
{
    // ── Shared doubles ────────────────────────────────────────────────────────

    private readonly Mock<IQueueRepository> _queueRepositoryMock = new();

    private readonly GetMyQueuesQueryHandler _handler;

    // ── Shared test data ──────────────────────────────────────────────────────

    private static readonly Guid UserId         = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid QueueId        = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrganisationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public GetMyQueuesQueryHandlerTests()
    {
        // Default: one active entry
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid, string, int, string)>
            {
                (QueueId, OrganisationId, "Morning Queue", 7, "Waiting")
            });

        _handler = new GetMyQueuesQueryHandler(_queueRepositoryMock.Object);
    }

    // ── Happy path — single entry ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithOneActiveEntry_ReturnsOneMyQueueEntry()
    {
        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithOneActiveEntry_PropagatesQueueId()
    {
        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result[0].QueueId.Should().Be(QueueId);
    }

    [Fact]
    public async Task Handle_WithOneActiveEntry_PropagatesOrganisationId()
    {
        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result[0].OrganisationId.Should().Be(OrganisationId);
    }

    [Fact]
    public async Task Handle_WithOneActiveEntry_PropagatesQueueName()
    {
        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result[0].QueueName.Should().Be("Morning Queue");
    }

    [Fact]
    public async Task Handle_WithOneActiveEntry_PropagatesTicketNumber()
    {
        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result[0].TicketNumber.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WithOneActiveEntry_PropagatesStatus()
    {
        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result[0].QueueStatus.Should().Be("Waiting");
    }

    // ── Multiple entries ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithMultipleActiveEntries_ReturnsAllMapped()
    {
        var queueId2 = Guid.NewGuid();
        var orgId2   = Guid.NewGuid();

        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid, string, int, string)>
            {
                (QueueId,  OrganisationId, "Morning Queue",  7,  "Waiting"),
                (queueId2, orgId2,         "Evening Queue",  12, "Serving")
            });

        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result.Should().HaveCount(2);
        result[1].QueueId.Should().Be(queueId2);
        result[1].QueueName.Should().Be("Evening Queue");
        result[1].TicketNumber.Should().Be(12);
        result[1].QueueStatus.Should().Be("Serving");
    }

    // ── Empty result ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoActiveEntries_ReturnsEmptyList()
    {
        _queueRepositoryMock
            .Setup(r => r.GetUserActiveEntriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid, string, int, string)>());

        var result = await _handler.Handle(new GetMyQueuesQuery(UserId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── UserId forwarding ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ForwardsUserIdToRepository()
    {
        var specificUserId = Guid.NewGuid();

        await _handler.Handle(new GetMyQueuesQuery(specificUserId), CancellationToken.None);

        _queueRepositoryMock.Verify(
            r => r.GetUserActiveEntriesAsync(specificUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
