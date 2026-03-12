using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NextTurn.IntegrationTests;

namespace NextTurn.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the "login" rate-limiting policy.
///
/// WHY a separate class?
/// ---------------------
/// The rate-limiter in ASP.NET Core is an in-memory sliding-window counter scoped
/// to the lifetime of the web server. IClassFixture gives each test CLASS its own
/// WebApplicationFactory, and therefore its own server with a fresh counter. Keeping
/// the 429 test in a dedicated class guarantees:
///   a) The counter starts at 0 — previous login calls from other test classes
///      cannot accidentally exhaust the 10-request budget.
///   b) The intent of the test is self-documenting — the whole class is about
///      rate limiting, not happy-path login behaviour.
///
/// How the rate limiter is configured (Program.cs):
///   - Sliding window: 10 requests / 60 s per client IP
///   - QueueLimit = 0 → excess requests are rejected immediately (HTTP 429)
///   - Applied only to POST /api/auth/login via [EnableRateLimiting("login")]
///
/// Why 11 requests satisfy the test:
///   The PermitLimit is 10. Requests 1-10 must all succeed with a non-429 status.
///   Request 11 must be rejected with HTTP 429 Too Many Requests.
///   Using an email that has no registered account means every successful request
///   returns HTTP 400 (Invalid credentials) — that is intentional. The rate limiter
///   counts requests regardless of whether the handler returns 2xx or 4xx.
///
/// Covered scenarios:
///   1. Eleven rapid POST /api/auth/login calls → 11th returns 429.
/// </summary>
[Collection("Integration")]
public sealed class LoginRateLimitIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public LoginRateLimitIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Rate limit ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ElevenRapidRequests_EleventhReturns429()
    {
        // Use an email that is not registered — every request will return 400
        // from the handler, but the rate limiter still counts each attempt.
        // This avoids having to set up / tear down a real user account.
        const string email    = "ratelimit-probe@example.com";
        const string password = "AnyPassword1!";
        const int    limit    = 10;

        var responses = new List<HttpResponseMessage>(limit + 1);

        // Send limit + 1 requests sequentially (not parallel) so the sliding window
        // counter increments predictably. Parallel requests could race and all land
        // inside the same window segment simultaneously, but sequential is deterministic.
        for (int i = 0; i <= limit; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new LoginPayload(email, password))
            };
            request.Headers.Add("X-Tenant-Id", TenantA.ToString());

            responses.Add(await _client.SendAsync(request));
        }

        // Requests 1–10 must NOT be rate-limited.
        // They return 400 (invalid credentials) because the user does not exist,
        // but that is a handler-level response — the rate limiter let them through.
        for (int i = 0; i < limit; i++)
        {
            responses[i].StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
                $"request {i + 1} is within the 10-request permit limit and must not be rate-limited");
        }

        // The 11th request must be rate-limited.
        responses[limit].StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "the 11th request exceeds the 10-request sliding-window limit and must be rejected with 429");
    }

    // ── Local DTO mirror ──────────────────────────────────────────────────────

    private sealed record LoginPayload(string Email, string Password);
}
