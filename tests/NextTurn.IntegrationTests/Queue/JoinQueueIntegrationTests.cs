using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using NextTurn.Domain.Auth;
using NextTurn.IntegrationTests;

namespace NextTurn.IntegrationTests.Queue;

/// <summary>
/// Integration tests for POST /api/queues/{queueId}/join.
///
/// Each test resets the database via Respawn and starts from a known state
/// built by <see cref="NextTurnWebApplicationFactory.SeedQueueAsync"/>.
///
/// Covered scenarios:
///   1. Authenticated user joins a valid queue → 200, returns ticket/position/eta
///   2. No JWT → 401 Unauthorized (falls through to FallbackPolicy)
///   3. Queue not found (unknown GUID) → 400 "Queue not found."
///   4. Same user joins twice → 409 Conflict "Already in this queue."
///   5. Queue at capacity → 409 with canBookAppointment: true
///   6. Malformed queueId GUID in route → 422 Unprocessable Entity
/// </summary>
[Collection("Integration")]
public sealed class JoinQueueIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    // These are set in InitializeAsync, after the database is seeded.
    private Guid _tenantId;
    private Guid _queueId;

    // A stable userId embedded in the JWT so the handler can identify the caller.
    private static readonly Guid TestUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public JoinQueueIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _queueId) = await _factory.SeedQueueAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task JoinQueue_WithValidRequest_Returns200()
    {
        var response = await PostJoinAsync(_queueId, TestUserId, _tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JoinQueue_WithValidRequest_ReturnsTicketPositionAndEta()
    {
        var response = await PostJoinAsync(_queueId, TestUserId, _tenantId);
        var body     = await ReadBodyAsync(response);

        body.Should().ContainKey("ticketNumber");
        body.Should().ContainKey("positionInQueue");
        body.Should().ContainKey("estimatedWaitSeconds");

        body["ticketNumber"].GetInt32().Should().Be(1,
            "the queue is empty so the first ticket is always #1");
        body["positionInQueue"].GetInt32().Should().Be(1,
            "the user is the only one in the queue");
        body["estimatedWaitSeconds"].GetInt32().Should().Be(300,
            "avgServiceTime=300s × position=1 = 300s");
    }

    // ── 2. No JWT → 401 ───────────────────────────────────────────────────────

    [Fact]
    public async Task JoinQueue_WithoutJwt_Returns401()
    {
        // Create a plain (unauthenticated) client.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());

        var response = await client.PostAsync($"/api/queues/{_queueId}/join", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 3. Queue not found → 400 ──────────────────────────────────────────────

    [Fact]
    public async Task JoinQueue_WithUnknownQueueId_Returns400QueueNotFound()
    {
        var unknownQueueId = Guid.NewGuid();

        var response = await PostJoinAsync(unknownQueueId, TestUserId, _tenantId);
        var body     = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body["detail"].GetString().Should().Be("Queue not found.");
    }

    // ── 4. Duplicate join → 409 Conflict ─────────────────────────────────────

    [Fact]
    public async Task JoinQueue_WhenUserAlreadyJoined_Returns409Conflict()
    {
        // First join should succeed.
        var first = await PostJoinAsync(_queueId, TestUserId, _tenantId);
        first.StatusCode.Should().Be(HttpStatusCode.OK,
            "pre-condition: first join must succeed");

        // Second join for the same user should be rejected.
        var second = await PostJoinAsync(_queueId, TestUserId, _tenantId);
        var body   = await ReadBodyAsync(second);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body["detail"].GetString().Should().Be("Already in this queue.");
    }

    // ── 5. Queue full → 409 with canBookAppointment ───────────────────────────

    [Fact]
    public async Task JoinQueue_WhenQueueFull_Returns409WithCanBookAppointment()
    {
        // Seed a separate queue with capacity=1 for this scenario.
        var (tenantId, smallQueueId) = await _factory.SeedQueueAsync(maxCapacity: 1);

        // First user fills the queue.
        var firstUserId = Guid.NewGuid();
        var first = await PostJoinAsync(smallQueueId, firstUserId, tenantId);
        first.StatusCode.Should().Be(HttpStatusCode.OK,
            "pre-condition: first user must join successfully");

        // Second user should hit the full-queue guard.
        var secondUserId = Guid.NewGuid();
        var second = await PostJoinAsync(smallQueueId, secondUserId, tenantId);
        var body   = await ReadBodyAsync(second);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body["canBookAppointment"].GetBoolean().Should()
            .BeTrue("the frontend uses this flag to surface the appointment booking CTA");
    }

    // ── 6. Empty GUID → 422 (FluentValidation) ───────────────────────────────

    [Fact]
    public async Task JoinQueue_WithEmptyGuidQueueId_Returns422()
    {
        // Guid.Empty passes the {:guid} route constraint but fails
        // JoinQueueCommandValidator's NotEmpty() rule → ValidationBehavior → 422.
        var response = await PostJoinAsync(Guid.Empty, TestUserId, _tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Posts to the join endpoint with a freshly minted JWT for the given user.
    /// The JWT embeds <paramref name="userId"/> as the <c>sub</c> claim and
    /// <paramref name="tenantId"/> as the <c>tid</c> claim.
    /// </summary>
    private Task<HttpResponseMessage> PostJoinAsync(
        Guid queueId,
        Guid userId,
        Guid tenantId)
    {
        var client = AuthenticatedClient(userId, tenantId);
        return client.PostAsync($"/api/queues/{queueId}/join", null);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> bearing a signed JWT for the given userId/tenantId
    /// and adds the required X-Tenant-Id header.
    /// </summary>
    private HttpClient AuthenticatedClient(Guid userId, Guid tenantId)
    {
        var token  = _factory.CreateTokenForRole(UserRole.User, userId: userId, tenantId: tenantId);
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
