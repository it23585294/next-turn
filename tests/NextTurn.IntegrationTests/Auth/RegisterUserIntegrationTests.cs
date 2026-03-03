using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NextTurn.IntegrationTests;

namespace NextTurn.IntegrationTests.Auth;

/// <summary>
/// Integration tests for POST /api/auth/register.
///
/// Each test gets a fully running application backed by a real SQL Server container.
/// Respawn resets all table data between tests so tests are independent and order-agnostic.
///
/// Covered scenarios:
///   1. Happy path — valid payload + valid X-Tenant-Id → 201 Created
///   2. Duplicate email in same tenant → 400 (domain business rule)
///   3. Cross-tenant isolation — same email, different tenants → both 201 (unique index is per-tenant)
///   4. Validation failure — weak password → 422 Unprocessable Entity
///   5. Missing X-Tenant-Id header → 400 (TenantMiddleware)
///   6. Malformed body / empty required fields → 422
/// </summary>
[Collection("Integration")]
public sealed class RegisterUserIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public RegisterUserIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Reset all table data before every test method — ensures full isolation.
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. Happy path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidPayload_Returns201Created()
    {
        var response = await PostRegisterAsync(ValidPayload(), TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithOptionalPhoneOmitted_Returns201Created()
    {
        var payload = ValidPayload() with { Phone = null };

        var response = await PostRegisterAsync(payload, TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── 2. Duplicate email same tenant ────────────────────────────────────────

    [Fact]
    public async Task Register_SameEmailSameTenant_SecondRequestReturns400()
    {
        // First registration succeeds
        var first = await PostRegisterAsync(ValidPayload(), TenantA);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second with identical email + tenant is rejected
        var second = await PostRegisterAsync(ValidPayload(), TenantA);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await ReadProblemDetailsAsync(second);
        body.Should().ContainKey("detail");
        body["detail"]!.ToString().Should().Contain("already in use");
    }

    // ── 3. Cross-tenant isolation (key test) ──────────────────────────────────

    [Fact]
    public async Task Register_SameEmailDifferentTenants_BothReturn201()
    {
        // The unique index is on (TenantId, Email_Value) — not just (Email_Value).
        // A user with the same email CAN exist in two different organisations.
        // This test fails if the global query filter or the unique index were
        // mistakenly scoped globally instead of per-tenant.
        var payload = ValidPayload(email: "shared@example.com");

        var responseA = await PostRegisterAsync(payload, TenantA);
        var responseB = await PostRegisterAsync(payload, TenantB);

        responseA.StatusCode.Should().Be(HttpStatusCode.Created,
            "TenantA should be able to register shared@example.com");
        responseB.StatusCode.Should().Be(HttpStatusCode.Created,
            "TenantB should independently register the same email — tenants are isolated");
    }

    // ── 4. Validation failure — 422 ───────────────────────────────────────────

    [Fact]
    public async Task Register_WithWeakPassword_Returns422WithErrors()
    {
        var payload = ValidPayload() with { Password = "weak" };

        var response = await PostRegisterAsync(payload, TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = await ReadProblemDetailsAsync(response);
        body.Should().ContainKey("errors");
    }

    [Fact]
    public async Task Register_WithEmptyName_Returns422()
    {
        var payload = ValidPayload() with { Name = string.Empty };

        var response = await PostRegisterAsync(payload, TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_WithInvalidEmailFormat_Returns422()
    {
        var payload = ValidPayload() with { Email = "not-an-email" };

        var response = await PostRegisterAsync(payload, TenantA);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── 5. Missing X-Tenant-Id ────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithoutTenantIdHeader_Returns400()
    {
        // TenantMiddleware must reject requests with no resolvable tenant.
        var response = await _client.PostAsJsonAsync("/api/auth/register", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMalformedTenantIdHeader_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(ValidPayload())
        };
        request.Headers.Add("X-Tenant-Id", "not-a-guid");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> PostRegisterAsync(RegisterPayload payload, Guid tenantId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Tenant-Id", tenantId.ToString());
        return _client.SendAsync(request);
    }

    private static async Task<Dictionary<string, JsonElement>> ReadProblemDetailsAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static RegisterPayload ValidPayload(
        string name = "Alice Smith",
        string email = "alice@example.com",
        string? phone = null,
        string password = "SecureP@ss1") =>
        new(name, email, phone, password);

    // Simple mirror of the API's RegisterRequest DTO — avoids a project reference
    // to the API layer's internal model types.
    private sealed record RegisterPayload(string Name, string Email, string? Phone, string Password);
}
