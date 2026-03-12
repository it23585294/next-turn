using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using NextTurn.Domain.Auth;
using NextTurn.IntegrationTests;

namespace NextTurn.IntegrationTests.Queue;

/// <summary>
/// Integration tests for GET /api/queues/{queueId}/status.
///
/// Tests exercise the polling endpoint called by the frontend every 30 seconds.
/// Each test starts from a clean database seeded with one org + queue via Respawn.
///
/// Covered scenarios:
///   1.  User joins, then polls status → 200 + ticketNumber=1 + positionInQueue=1
///   2.  User joins, then polls status → 200 + estimatedWaitSeconds = 1 × avgServiceTimeSecs
///   3.  User joins, then polls status → 200 + queueStatus = "Active"
///   4.  User polls without having joined → 400 "No active queue entry found."
///   5.  Unknown queueId → 400 "Queue not found."
///   6.  No JWT → 401 Unauthorized (FallbackPolicy)
///   7.  Two users join the same queue; second user's status → positionInQueue = 2
///   8.  Two users join; second user's ETA = 2 × avgServiceTimeSecs
/// </summary>
[Collection("Integration")]
public sealed class GetQueueStatusIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    // Seeded in InitializeAsync.
    private Guid _tenantId;
    private Guid _queueId;

    // Average service time used when seeding — must match expected ETA assertions.
    private const int AvgServiceTimeSecs = 300;

    // Stable user IDs so JWT sub claims are deterministic across helper calls.
    private static readonly Guid UserAId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserBId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    public GetQueueStatusIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _queueId) = await _factory.SeedQueueAsync(avgServiceTimeSecs: AvgServiceTimeSecs);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. Happy path — single user, first in queue ───────────────────────────

    [Fact]
    public async Task GetQueueStatus_AfterJoin_Returns200()
    {
        await JoinQueueAsync(_queueId, UserAId, _tenantId);

        var response = await GetStatusAsync(_queueId, UserAId, _tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQueueStatus_AfterJoin_TicketNumberIsOne()
    {
        await JoinQueueAsync(_queueId, UserAId, _tenantId);

        var body = await GetStatusBodyAsync(_queueId, UserAId, _tenantId);

        body.Should().ContainKey("ticketNumber");
        body["ticketNumber"].GetInt32().Should().Be(1,
            "the queue is empty so the first join always gets ticket #1");
    }

    [Fact]
    public async Task GetQueueStatus_AfterJoin_PositionInQueueIsOne()
    {
        await JoinQueueAsync(_queueId, UserAId, _tenantId);

        var body = await GetStatusBodyAsync(_queueId, UserAId, _tenantId);

        body.Should().ContainKey("positionInQueue");
        body["positionInQueue"].GetInt32().Should().Be(1,
            "the user is the only active entry in the queue");
    }

    [Fact]
    public async Task GetQueueStatus_AfterJoin_EstimatedWaitSeconds_EqualsOneAvgServicePeriod()
    {
        await JoinQueueAsync(_queueId, UserAId, _tenantId);

        var body = await GetStatusBodyAsync(_queueId, UserAId, _tenantId);

        body.Should().ContainKey("estimatedWaitSeconds");
        body["estimatedWaitSeconds"].GetInt32().Should().Be(AvgServiceTimeSecs,
            "ETA = position(1) × avgServiceTimeSecs(300) = 300");
    }

    [Fact]
    public async Task GetQueueStatus_AfterJoin_QueueStatusIsActive()
    {
        await JoinQueueAsync(_queueId, UserAId, _tenantId);

        var body = await GetStatusBodyAsync(_queueId, UserAId, _tenantId);

        body.Should().ContainKey("queueStatus");
        body["queueStatus"].GetString().Should().Be("Active");
    }

    // ── 4. No active entry → 400 ──────────────────────────────────────────────

    [Fact]
    public async Task GetQueueStatus_WithoutJoining_Returns400()
    {
        // UserA never joined — any GET for this user should return 400.
        var response = await GetStatusAsync(_queueId, UserAId, _tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQueueStatus_WithoutJoining_Returns400WithExpectedMessage()
    {
        var response = await GetStatusAsync(_queueId, UserAId, _tenantId);
        var body     = await ReadBodyAsync(response);

        body["detail"].GetString().Should().Be("No active queue entry found.");
    }

    // ── 5. Unknown queueId → 400 "Queue not found." ───────────────────────────

    [Fact]
    public async Task GetQueueStatus_WithUnknownQueueId_Returns400QueueNotFound()
    {
        var unknownQueueId = Guid.NewGuid();

        var response = await GetStatusAsync(unknownQueueId, UserAId, _tenantId);
        var body     = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body["detail"].GetString().Should().Be("Queue not found.");
    }

    // ── 6. No JWT → 401 ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetQueueStatus_WithoutJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());

        var response = await client.GetAsync($"/api/queues/{_queueId}/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 7–8. Two users join the same queue ───────────────────────────────────
    //
    // User A joins first (ticket #1, position 1).
    // User B joins second (ticket #2, position 2).
    //
    // GetUserPositionAsync counts active entries with TicketNumber ≤ user's ticket:
    //   - For User B (ticket=2): entries with ticket ≤ 2 are [1, 2] → position = 2.
    //
    // This validates the "shrinks as served" position logic end-to-end.

    [Fact]
    public async Task GetQueueStatus_SecondUserInQueue_PositionInQueueIsTwo()
    {
        // User A joins first.
        await JoinQueueAsync(_queueId, UserAId, _tenantId);

        // User B joins second.
        await JoinQueueAsync(_queueId, UserBId, _tenantId);

        // User B's status should reflect position 2.
        var body = await GetStatusBodyAsync(_queueId, UserBId, _tenantId);

        body["positionInQueue"].GetInt32().Should().Be(2,
            "User B joined after User A, so User B is at position 2");
    }

    [Fact]
    public async Task GetQueueStatus_SecondUserInQueue_EstimatedWaitSeconds_IsTwoAvgServicePeriods()
    {
        await JoinQueueAsync(_queueId, UserAId, _tenantId);
        await JoinQueueAsync(_queueId, UserBId, _tenantId);

        var body = await GetStatusBodyAsync(_queueId, UserBId, _tenantId);

        body["estimatedWaitSeconds"].GetInt32().Should().Be(2 * AvgServiceTimeSecs,
            "ETA = position(2) × avgServiceTimeSecs(300) = 600");
    }

    [Fact]
    public async Task GetQueueStatus_FirstUserInQueue_PositionRemainsOne_WhenSecondUserJoinsAfter()
    {
        // User A joins first — position should stay 1 even after User B joins.
        await JoinQueueAsync(_queueId, UserAId, _tenantId);
        await JoinQueueAsync(_queueId, UserBId, _tenantId);

        var body = await GetStatusBodyAsync(_queueId, UserAId, _tenantId);

        body["positionInQueue"].GetInt32().Should().Be(1,
            "User A still has the lowest ticket number — they are still first in line");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> JoinQueueAsync(Guid queueId, Guid userId, Guid tenantId)
    {
        var client = AuthenticatedClient(userId, tenantId, UserRole.User);
        return client.PostAsync($"/api/queues/{queueId}/join", null);
    }

    private Task<HttpResponseMessage> GetStatusAsync(Guid queueId, Guid userId, Guid tenantId)
    {
        var client = AuthenticatedClient(userId, tenantId, UserRole.User);
        return client.GetAsync($"/api/queues/{queueId}/status");
    }

    private async Task<Dictionary<string, JsonElement>> GetStatusBodyAsync(
        Guid queueId,
        Guid userId,
        Guid tenantId)
    {
        var response = await GetStatusAsync(queueId, userId, tenantId);
        return await ReadBodyAsync(response);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with:
    ///   - Bearer JWT embedding <paramref name="userId"/> as <c>sub</c> and
    ///     <paramref name="tenantId"/> as <c>tid</c>
    ///   - X-Tenant-Id header for TenantMiddleware
    /// </summary>
    private HttpClient AuthenticatedClient(Guid userId, Guid tenantId, UserRole role)
    {
        var token  = _factory.CreateTokenForRole(role, userId: userId, tenantId: tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    private static async Task<Dictionary<string, JsonElement>> ReadBodyAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
