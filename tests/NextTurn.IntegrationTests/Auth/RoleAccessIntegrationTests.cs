using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NextTurn.Domain.Auth;

namespace NextTurn.IntegrationTests.Auth;

/// <summary>
/// Integration tests for NT-12 role-based access control.
///
/// Verifies the full role × policy matrix by calling the four probe endpoints
/// (/api/probe/user, /api/probe/staff, /api/probe/org-admin, /api/probe/system-admin)
/// with tokens minted directly via NextTurnWebApplicationFactory.CreateTokenForRole,
/// avoiding any dependency on the login endpoint.
///
/// Coverage:
///   AC1 — No token                    → 401 on any protected endpoint
///   AC1 — Expired token               → 401
///   AC1 — Tampered token              → 401
///   AC2 — User   → staff endpoint     → 403
///   AC2 — User   → org-admin endpoint → 403
///   AC2 — User   → system-admin       → 403
///   AC3 — Staff  → org-admin          → 403
///   AC3 — Staff  → system-admin       → 403
///   AC4 — OrgAdmin → system-admin     → 403
///   AC5 — SystemAdmin → all four      → 200
///   AC6 — Register + login are [AllowAnonymous] → no token needed → not 401
///   Role body — response carries the caller's role string
/// </summary>
[Collection("Integration")]
public sealed class RoleAccessIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public RoleAccessIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── AC1: 401 when there is no valid token ─────────────────────────────────

    [Theory]
    [InlineData("/api/probe/user")]
    [InlineData("/api/probe/staff")]
    [InlineData("/api/probe/org-admin")]
    [InlineData("/api/probe/system-admin")]
    public async Task NoToken_Returns401(string endpoint)
    {
        var client = _factory.CreateClient();    // no Authorization header

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "every protected endpoint must reject requests with no token");
    }

    [Theory]
    [InlineData("/api/probe/user")]
    [InlineData("/api/probe/staff")]
    [InlineData("/api/probe/org-admin")]
    [InlineData("/api/probe/system-admin")]
    public async Task ExpiredToken_Returns401(string endpoint)
    {
        // Mint a token with a negative expiry (already past)
        var token = _factory.CreateTokenForRole(UserRole.SystemAdmin, expiryMinutes: -1);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "an expired token must be rejected");
    }

    [Theory]
    [InlineData("/api/probe/user")]
    [InlineData("/api/probe/staff")]
    [InlineData("/api/probe/org-admin")]
    [InlineData("/api/probe/system-admin")]
    public async Task TamperedToken_Returns401(string endpoint)
    {
        // Take a valid token and corrupt the signature segment
        var valid  = _factory.CreateTokenForRole(UserRole.SystemAdmin);
        var parts  = valid.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}.invalidsignatureXXXX";

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "a token with an invalid signature must be rejected");
    }

    // ── AC2: User role is denied staff/admin endpoints ────────────────────────

    [Theory]
    [InlineData("/api/probe/staff")]
    [InlineData("/api/probe/org-admin")]
    [InlineData("/api/probe/system-admin")]
    public async Task UserRole_DeniedHigherProbeEndpoints_Returns403(string endpoint)
    {
        var client = _factory.CreateClientForRole(UserRole.User, tenantId: TenantA);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            because: $"a User does not have the role required for {endpoint}");
    }

    [Fact]
    public async Task UserRole_CanAccessUserProbeEndpoint_Returns200()
    {
        var client = _factory.CreateClientForRole(UserRole.User, tenantId: TenantA);

        var response = await client.GetAsync("/api/probe/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── AC3: Staff role is denied org-admin/system-admin endpoints ────────────

    [Theory]
    [InlineData("/api/probe/org-admin")]
    [InlineData("/api/probe/system-admin")]
    public async Task StaffRole_DeniedAdminProbeEndpoints_Returns403(string endpoint)
    {
        var client = _factory.CreateClientForRole(UserRole.Staff, tenantId: TenantA);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            because: $"a Staff member does not have the role required for {endpoint}");
    }

    [Theory]
    [InlineData("/api/probe/user")]
    [InlineData("/api/probe/staff")]
    public async Task StaffRole_CanAccessUserAndStaffProbeEndpoints_Returns200(string endpoint)
    {
        var client = _factory.CreateClientForRole(UserRole.Staff, tenantId: TenantA);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── AC4: OrgAdmin is denied system-admin endpoint ─────────────────────────

    [Fact]
    public async Task OrgAdminRole_DeniedSystemAdminEndpoint_Returns403()
    {
        var client = _factory.CreateClientForRole(UserRole.OrgAdmin, tenantId: TenantA);

        var response = await client.GetAsync("/api/probe/system-admin");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            because: "an OrgAdmin must not access the SystemAdmin endpoint");
    }

    [Theory]
    [InlineData("/api/probe/user")]
    [InlineData("/api/probe/staff")]
    [InlineData("/api/probe/org-admin")]
    public async Task OrgAdminRole_CanAccessUserStaffAndOrgAdminEndpoints_Returns200(string endpoint)
    {
        var client = _factory.CreateClientForRole(UserRole.OrgAdmin, tenantId: TenantA);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── AC5: SystemAdmin has full access ──────────────────────────────────────

    [Theory]
    [InlineData("/api/probe/user")]
    [InlineData("/api/probe/staff")]
    [InlineData("/api/probe/org-admin")]
    [InlineData("/api/probe/system-admin")]
    public async Task SystemAdminRole_CanAccessAllProbeEndpoints_Returns200(string endpoint)
    {
        var client = _factory.CreateClientForRole(UserRole.SystemAdmin, tenantId: TenantA);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"SystemAdmin should have full access to {endpoint}");
    }

    // ── AC6: Register and login are [AllowAnonymous] ──────────────────────────

    [Fact]
    public async Task RegisterEndpoint_WithNoToken_IsNotRejectedWith401()
    {
        // We're not submitting a valid payload — we just want to confirm the endpoint
        // is reachable (not blocked by authentication). 422 (validation) or 400 is fine.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            because: "POST /api/auth/register must be publicly accessible");
    }

    [Fact]
    public async Task LoginEndpoint_WithNoToken_IsNotRejectedWith401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            because: "POST /api/auth/login must be publicly accessible");
    }

    // ── Response body carries caller's role ───────────────────────────────────

    [Theory]
    [InlineData(UserRole.User,        "/api/probe/user",        "User")]
    [InlineData(UserRole.Staff,       "/api/probe/staff",       "Staff")]
    [InlineData(UserRole.OrgAdmin,    "/api/probe/org-admin",   "OrgAdmin")]
    [InlineData(UserRole.SystemAdmin, "/api/probe/system-admin","SystemAdmin")]
    public async Task ProbeEndpoint_Returns200WithCallerRole(
        UserRole role, string endpoint, string expectedRole)
    {
        var client = _factory.CreateClientForRole(role, tenantId: TenantA);

        var response = await client.GetAsync(endpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProbeBody>();
        body!.Role.Should().Be(expectedRole,
            because: "the probe endpoint echoes the caller's role from the JWT claim");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed record ProbeBody(string Role);
}
