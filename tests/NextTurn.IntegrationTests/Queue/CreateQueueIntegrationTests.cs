using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NextTurn.Domain.Auth;
using NextTurn.IntegrationTests;

namespace NextTurn.IntegrationTests.Queue;

/// <summary>
/// Integration tests for POST /api/queues — OrgAdmin creates a queue.
///
/// Database is seeded per-test via Respawn: each test gets a clean state.
/// <see cref="NextTurnWebApplicationFactory.SeedQueueAsync"/> creates a real
/// Organisation row so the handler's "verify org exists" step (step 1) passes.
///
/// Covered scenarios:
///   1.  OrgAdmin JWT → 201 Created
///   2.  OrgAdmin JWT → body contains non-empty queueId
///   3.  OrgAdmin JWT → body contains shareableLink in the expected format
///   4.  OrgAdmin JWT → 201 Location header points to the new queue status endpoint
///   5.  SystemAdmin JWT → 201 (also authorised by the Roles policy)
///   6.  User role JWT → 403 Forbidden (role not in OrgAdmin,SystemAdmin)
///   7.  Staff role JWT → 403 Forbidden
///   8.  No JWT → 401 Unauthorized (FallbackPolicy)
///   9.  Missing Name → 422 Unprocessable Entity (FluentValidation)
///   10. MaxCapacity = 0 → 422 Unprocessable Entity
///   11. AverageServiceTimeSeconds = 0 → 422 Unprocessable Entity
/// </summary>
[Collection("Integration")]
public sealed class CreateQueueIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    // Populated in InitializeAsync after the database is seeded.
    // _tenantId == the seeded Organisation.Id (which doubles as the OrgAdmin's TenantId).
    private Guid _tenantId;

    public CreateQueueIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        // SeedQueueAsync creates an Organisation + one Queue.
        // We only need the Organisation here, but SeedQueueAsync is the
        // established seed helper — the extra queue row is harmless.
        (_tenantId, _) = await _factory.SeedQueueAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1–4. OrgAdmin JWT — status code + response body ──────────────────────

    [Fact]
    public async Task CreateQueue_OrgAdminJwt_Returns201Created()
    {
        var response = await PostCreateQueueAsync(UserRole.OrgAdmin);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateQueue_OrgAdminJwt_ResponseBodyContainsNonEmptyQueueId()
    {
        var response = await PostCreateQueueAsync(UserRole.OrgAdmin);
        var body     = await ReadBodyAsync(response);

        body.Should().ContainKey("queueId");
        body["queueId"].GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateQueue_OrgAdminJwt_ResponseBodyContainsShareableLink()
    {
        var response = await PostCreateQueueAsync(UserRole.OrgAdmin);
        var body     = await ReadBodyAsync(response);

        body.Should().ContainKey("shareableLink");
        body["shareableLink"].GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateQueue_OrgAdminJwt_ShareableLinkMatchesExpectedFormat()
    {
        var response = await PostCreateQueueAsync(UserRole.OrgAdmin);
        var body     = await ReadBodyAsync(response);

        var queueId       = body["queueId"].GetGuid();
        var shareableLink = body["shareableLink"].GetString();

        // Format: /queues/{tenantId}/{queueId}
        shareableLink.Should().Be($"/queues/{_tenantId}/{queueId}");
    }

    [Fact]
    public async Task CreateQueue_OrgAdminJwt_LocationHeaderPointsToQueueStatusEndpoint()
    {
        var response = await PostCreateQueueAsync(UserRole.OrgAdmin);

        // CreatedAtAction sets the Location header to GET /api/queues/{queueId}/status.
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should()
            .Contain("/api/queues/")
            .And.Contain("/status");
    }

    // ── 5. SystemAdmin JWT → 201 (also in the allowed roles) ─────────────────

    [Fact]
    public async Task CreateQueue_SystemAdminJwt_Returns201Created()
    {
        var response = await PostCreateQueueAsync(UserRole.SystemAdmin);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── 6–7. Forbidden roles → 403 ────────────────────────────────────────────

    [Fact]
    public async Task CreateQueue_UserRoleJwt_Returns403Forbidden()
    {
        var response = await PostCreateQueueAsync(UserRole.User);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateQueue_StaffRoleJwt_Returns403Forbidden()
    {
        var response = await PostCreateQueueAsync(UserRole.Staff);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── 8. No JWT → 401 ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateQueue_WithoutJwt_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        // X-Tenant-Id is required by TenantMiddleware (which runs before UseAuthorization).
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());

        var response = await client.PostAsJsonAsync("/api/queues", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 9–11. Validation failures → 422 ──────────────────────────────────────

    [Fact]
    public async Task CreateQueue_WithEmptyName_Returns422()
    {
        var response = await PostCreateQueueAsync(
            UserRole.OrgAdmin,
            request: new { Name = "", MaxCapacity = 10, AverageServiceTimeSeconds = 60 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateQueue_WithZeroMaxCapacity_Returns422()
    {
        var response = await PostCreateQueueAsync(
            UserRole.OrgAdmin,
            request: new { Name = "Counter", MaxCapacity = 0, AverageServiceTimeSeconds = 60 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateQueue_WithZeroAverageServiceTime_Returns422()
    {
        var response = await PostCreateQueueAsync(
            UserRole.OrgAdmin,
            request: new { Name = "Counter", MaxCapacity = 10, AverageServiceTimeSeconds = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> PostCreateQueueAsync(
        UserRole role,
        object?  request = null)
    {
        var client = _factory.CreateClientForRole(role, tenantId: _tenantId);
        // X-Tenant-Id is required by TenantMiddleware.
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());

        return client.PostAsJsonAsync("/api/queues", request ?? ValidRequest());
    }

    private static object ValidRequest() =>
        new { Name = "Main Counter", MaxCapacity = 50, AverageServiceTimeSeconds = 180 };

    private static async Task<Dictionary<string, JsonElement>> ReadBodyAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
