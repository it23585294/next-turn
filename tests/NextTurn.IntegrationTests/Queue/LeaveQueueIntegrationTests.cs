using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Queue.Enums;
using NextTurn.IntegrationTests;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.IntegrationTests.Queue;

/// <summary>
/// Integration tests for POST /api/queues/{queueId}/leave.
///
/// Each test resets the database via Respawn and starts from a known state
/// built by <see cref="NextTurnWebApplicationFactory.SeedQueueAsync"/>.
///
/// Covered scenarios:
///   1. Authenticated user leaves a valid queue they're in → 204 No Content
///   2. Entry status persists as Cancelled in the database
///   3. Global consumer (TenantId = Guid.Empty) can leave queues too
///   4. No JWT → 401 Unauthorized
///   5. User not in queue → 400 "You are not in this queue."
///   6. Malformed queueId GUID in route → 422 Unprocessable Entity
///   7. Leave same queue twice → 400 on second attempt (user no longer in queue)
/// </summary>
[Collection("Integration")]
public sealed class LeaveQueueIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    // These are set in InitializeAsync, after the database is seeded.
    private Guid _tenantId;
    private Guid _queueId;

    // A stable userId embedded in the JWT so the handler can identify the caller.
    private static readonly Guid TestUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public LeaveQueueIntegrationTests(NextTurnWebApplicationFactory factory)
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
    public async Task LeaveQueue_WithValidRequest_Returns204NoContent()
    {
        // Pre-condition: User must be in the queue first
        await PostJoinAsync(_queueId, TestUserId, _tenantId);

        var response = await PostLeaveAsync(_queueId, TestUserId, _tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LeaveQueue_WithValidRequest_PersistsCancelledStatusInDatabase()
    {
        // Pre-condition: User joins
        var joinResponse = await PostJoinAsync(_queueId, TestUserId, _tenantId);
        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // User leaves
        var leaveResponse = await PostLeaveAsync(_queueId, TestUserId, _tenantId);
        leaveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Attempt to get status — should fail because user is no longer in queue
        var statusResponse = await GetStatusAsync(_queueId, TestUserId, _tenantId);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var entry = await db.QueueEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.QueueId == _queueId && e.UserId == TestUserId);

        entry.Should().NotBeNull();
        entry!.Status.Should().Be(QueueEntryStatus.Cancelled);
    }

    [Fact]
    public async Task LeaveQueue_WithGlobalConsumerUser_Returns204NoContent()
    {
        var consumerUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        // Consumer JWT carries tid = Guid.Empty while X-Tenant-Id scopes the queue request.
        var client = _factory.CreateClient();
        var token = _factory.CreateTokenForRole(UserRole.User, userId: consumerUserId, tenantId: Guid.Empty);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());

        var join = await client.PostAsync($"/api/queues/{_queueId}/join", null);
        join.StatusCode.Should().Be(HttpStatusCode.OK);

        var leave = await client.PostAsync($"/api/queues/{_queueId}/leave", null);
        leave.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── 2. No JWT → 401 ───────────────────────────────────────────────────────

    [Fact]
    public async Task LeaveQueue_WithoutJwt_Returns401()
    {
        // Create a plain (unauthenticated) client.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());

        var response = await client.PostAsync($"/api/queues/{_queueId}/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 3. User not in queue → 400 ────────────────────────────────────────────

    [Fact]
    public async Task LeaveQueue_WhenUserNotInQueue_Returns400()
    {
        var differentUserId = Guid.NewGuid();

        var response = await PostLeaveAsync(_queueId, differentUserId, _tenantId);
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body["detail"].GetString().Should().Be("You are not in this queue.");
    }

    // ── 4. Leave twice → 400 on second attempt ────────────────────────────────

    [Fact]
    public async Task LeaveQueue_WhenUserLeavesTwice_SecondReturns400()
    {
        // Precondition: User joins
        var joinResponse = await PostJoinAsync(_queueId, TestUserId, _tenantId);
        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // First leave should succeed
        var firstLeave = await PostLeaveAsync(_queueId, TestUserId, _tenantId);
        firstLeave.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Second leave for the same user should fail — user no longer in queue
        var secondLeave = await PostLeaveAsync(_queueId, TestUserId, _tenantId);
        var body = await ReadBodyAsync(secondLeave);

        secondLeave.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body["detail"].GetString().Should().Be("You are not in this queue.");
    }

    // ── 5. Empty GUID → 422 (FluentValidation) ────────────────────────────────

    [Fact]
    public async Task LeaveQueue_WithEmptyGuidQueueId_Returns422()
    {
        var response = await PostLeaveAsync(Guid.Empty, TestUserId, _tenantId);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Posts to the leave endpoint with a freshly minted JWT for the given user.
    /// </summary>
    private Task<HttpResponseMessage> PostLeaveAsync(
        Guid queueId,
        Guid userId,
        Guid tenantId)
    {
        var client = AuthenticatedClient(userId, tenantId);
        return client.PostAsync($"/api/queues/{queueId}/leave", null);
    }

    /// <summary>
    /// Gets the queue status with a freshly minted JWT for the given user.
    /// </summary>
    private Task<HttpResponseMessage> GetStatusAsync(
        Guid queueId,
        Guid userId,
        Guid tenantId)
    {
        var client = AuthenticatedClient(userId, tenantId);
        return client.GetAsync($"/api/queues/{queueId}/status");
    }

    /// <summary>
    /// Posts to the join endpoint with a freshly minted JWT for the given user.
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
        var token = _factory.CreateTokenForRole(UserRole.User, userId: userId, tenantId: tenantId);
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
