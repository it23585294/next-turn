using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NextTurn.IntegrationTests;

namespace NextTurn.IntegrationTests.Auth;

/// <summary>
/// Integration tests for POST /api/auth/login.
///
/// Each test resets the database via Respawn and starts from a known state.
/// The XUnit IClassFixture gives this class its own WebApplicationFactory instance,
/// which means its own in-process server — the rate-limiter bucket is isolated
/// from other test classes.
///
/// Covered scenarios:
///   1. Valid credentials → 200 with a non-empty accessToken
///   2. Wrong password → 400 "Invalid credentials." (generic message — no enumeration)
///   3. Non-existent email → 400 "Invalid credentials." (same message — no enumeration)
///   4. Three consecutive bad passwords → 4th attempt → 400 "Account is temporarily locked"
///   5. Missing X-Tenant-Id header → 400 (TenantMiddleware short-circuits before the handler)
///   6. Malformed X-Tenant-Id header → 400 (TenantMiddleware short-circuits before the handler)
///
/// Rate-limit (429) scenario is in LoginRateLimitIntegrationTests to keep the
/// bucket fresh and the intent clear.
/// </summary>
[Collection("Integration")]
public sealed class LoginUserIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private const string DefaultEmail    = "alice@example.com";
    private const string DefaultPassword = "SecureP@ss1";

    public LoginUserIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithAccessToken()
    {
        await RegisterAsync(DefaultEmail, DefaultPassword);

        var response = await PostLoginAsync(DefaultEmail, DefaultPassword, TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        body.Should().ContainKey("accessToken");
        body["accessToken"]!.ToString().Should().NotBeNullOrWhiteSpace(
            "a signed JWT must be returned so the client can authenticate subsequent requests");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ResponseContainsExpectedClaims()
    {
        await RegisterAsync(DefaultEmail, DefaultPassword);

        var response = await PostLoginAsync(DefaultEmail, DefaultPassword, TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        body.Should().ContainKey("userId");
        body.Should().ContainKey("name");
        body.Should().ContainKey("email");
        body.Should().ContainKey("role");

        body["email"]!.ToString().Should().Be(DefaultEmail);
        body["role"]!.ToString().Should().Be("User");
    }

    // ── 2. Wrong password ─────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithWrongPassword_Returns400WithGenericMessage()
    {
        await RegisterAsync(DefaultEmail, DefaultPassword);

        var response = await PostLoginAsync(DefaultEmail, "WrongPass!", TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await ReadBodyAsync(response);
        body.Should().ContainKey("detail");
        body["detail"]!.ToString().Should().Be("Invalid credentials.");
    }

    // ── 3. Non-existent email (no user enumeration) ───────────────────────────

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns400WithSameGenericMessage()
    {
        // IMPORTANT: the error message must be identical to the wrong-password case.
        // Returning a different message would allow an attacker to enumerate registered
        // email addresses (user enumeration attack).
        var response = await PostLoginAsync("ghost@example.com", DefaultPassword, TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await ReadBodyAsync(response);
        body.Should().ContainKey("detail");
        body["detail"]!.ToString().Should().Be("Invalid credentials.",
            "must match the wrong-password message exactly to prevent user enumeration");
    }

    // ── 4. Account lockout after threshold failures ───────────────────────────

    [Fact]
    public async Task Login_ThreeFailedAttempts_FourthReturns400AccountLocked()
    {
        // Arrange — register a real user so each attempt goes through the full handler flow.
        await RegisterAsync(DefaultEmail, DefaultPassword);

        // Three consecutive wrong-password attempts exhaust the threshold (RecordFailedLogin).
        for (int i = 0; i < 3; i++)
        {
            var attempt = await PostLoginAsync(DefaultEmail, "WrongPass!", TenantA);
            attempt.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                $"attempt {i + 1} should return 400 — account not yet locked");

            var attemptBody = await ReadBodyAsync(attempt);
            attemptBody["detail"]!.ToString().Should().Be("Invalid credentials.",
                $"attempt {i + 1} should use the generic message, not reveal imminent lockout");
        }

        // The 4th attempt (correct password irrelevant — account is now locked) should return
        // a distinct message so the user knows why they're blocked.
        var fourth = await PostLoginAsync(DefaultEmail, DefaultPassword, TenantA);

        fourth.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var fourthBody = await ReadBodyAsync(fourth);
        fourthBody["detail"]!.ToString().Should().Contain("temporarily locked",
            "lockout message must inform the user to try again later");
    }

    // ── 5. Missing X-Tenant-Id ────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithoutTenantIdHeader_Returns400()
    {
        // TenantMiddleware short-circuits with 400 before the request reaches
        // the Login handler or the rate limiter — no need to register a user.
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginPayload(DefaultEmail, DefaultPassword));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 6. Malformed X-Tenant-Id ─────────────────────────────────────────────

    [Fact]
    public async Task Login_WithMalformedTenantIdHeader_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginPayload(DefaultEmail, DefaultPassword))
        };
        request.Headers.Add("X-Tenant-Id", "not-a-guid");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task RegisterAsync(string email, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new RegisterPayload("Alice Smith", email, null, password))
        };
        request.Headers.Add("X-Tenant-Id", TenantA.ToString());

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"pre-condition: registering {email} must succeed before the login tests can run");
    }

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password, Guid tenantId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginPayload(email, password))
        };
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        return _client.SendAsync(request);
    }

    private static async Task<Dictionary<string, JsonElement>> ReadBodyAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    // Local mirrors of the API DTOs — keeps the test project free of an API
    // project reference while still sending correctly shaped JSON.
    private sealed record LoginPayload(string Email, string Password);
    private sealed record RegisterPayload(string Name, string Email, string? Phone, string Password);
}
