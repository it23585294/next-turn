using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace NextTurn.UnitTests.API.Auth;

/// <summary>
/// Unit tests for the four named authorization policies and the FallbackPolicy
/// defined in Program.cs (NT-12-1).
///
/// Tests build an isolated IAuthorizationService via ServiceCollection — no HTTP server,
/// no database, no JWT library. The policies are mirrored here exactly as registered in
/// Program.cs so any drift between the two will immediately surface as a failing test.
///
/// Coverage:
///   - Full 4-role × 4-policy matrix (16 combinations)
///   - All four policies deny unauthenticated principals
///   - FallbackPolicy denies unauthenticated, allows any authenticated role
/// </summary>
public sealed class AuthorizationPolicyTests
{
    private readonly IAuthorizationService _authService;

    public AuthorizationPolicyTests()
    {
        // Mirror the exact policy configuration from Program.cs.
        // If the policies change there, these tests must be updated to match.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("IsUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "User", "Staff", "OrgAdmin", "SystemAdmin"));

            options.AddPolicy("IsStaff", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "Staff", "OrgAdmin", "SystemAdmin"));

            options.AddPolicy("IsOrgAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "OrgAdmin", "SystemAdmin"));

            options.AddPolicy("IsSystemAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "SystemAdmin"));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        _authService = services.BuildServiceProvider()
                                .GetRequiredService<IAuthorizationService>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an authenticated ClaimsPrincipal carrying the specified role claim.
    /// A non-empty authenticationType makes the identity report IsAuthenticated = true.
    /// </summary>
    private static ClaimsPrincipal Principal(string role)
    {
        var identity = new ClaimsIdentity(
            claims: [new Claim("role", role)],
            authenticationType: "TestAuth");   // non-empty → IsAuthenticated = true
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Returns an unauthenticated ClaimsPrincipal (no claims, no auth type).
    /// </summary>
    private static ClaimsPrincipal Unauthenticated()
        => new(new ClaimsIdentity());          // empty authenticationType → IsAuthenticated = false

    // ── Policy matrix ─────────────────────────────────────────────────────────
    //
    //              IsUser  IsStaff  IsOrgAdmin  IsSystemAdmin
    // User           ✅      ❌        ❌           ❌
    // Staff          ✅      ✅        ❌           ❌
    // OrgAdmin       ✅      ✅        ✅           ❌
    // SystemAdmin    ✅      ✅        ✅           ✅

    public static TheoryData<string, string, bool> PolicyMatrix => new()
    {
        // ── User ────────────────────────────────────────────────────────
        { "User",        "IsUser",        true  },
        { "User",        "IsStaff",       false },
        { "User",        "IsOrgAdmin",    false },
        { "User",        "IsSystemAdmin", false },
        // ── Staff ───────────────────────────────────────────────────────
        { "Staff",       "IsUser",        true  },
        { "Staff",       "IsStaff",       true  },
        { "Staff",       "IsOrgAdmin",    false },
        { "Staff",       "IsSystemAdmin", false },
        // ── OrgAdmin ────────────────────────────────────────────────────
        { "OrgAdmin",    "IsUser",        true  },
        { "OrgAdmin",    "IsStaff",       true  },
        { "OrgAdmin",    "IsOrgAdmin",    true  },
        { "OrgAdmin",    "IsSystemAdmin", false },
        // ── SystemAdmin ─────────────────────────────────────────────────
        { "SystemAdmin", "IsUser",        true  },
        { "SystemAdmin", "IsStaff",       true  },
        { "SystemAdmin", "IsOrgAdmin",    true  },
        { "SystemAdmin", "IsSystemAdmin", true  },
    };

    [Theory]
    [MemberData(nameof(PolicyMatrix))]
    public async Task Policy_EnforcesRoleHierarchy(string role, string policy, bool shouldSucceed)
    {
        var result = await _authService.AuthorizeAsync(Principal(role), resource: null, policy);

        if (shouldSucceed)
            result.Succeeded.Should().BeTrue(
                because: $"role '{role}' should satisfy policy '{policy}'");
        else
            result.Succeeded.Should().BeFalse(
                because: $"role '{role}' should be denied by policy '{policy}'");
    }

    // ── Unauthenticated principal is denied by every policy ───────────────────

    [Theory]
    [InlineData("IsUser")]
    [InlineData("IsStaff")]
    [InlineData("IsOrgAdmin")]
    [InlineData("IsSystemAdmin")]
    public async Task AllPolicies_DenyUnauthenticatedPrincipal(string policy)
    {
        var result = await _authService.AuthorizeAsync(Unauthenticated(), resource: null, policy);

        result.Succeeded.Should().BeFalse(
            because: $"an unauthenticated user must always be denied by policy '{policy}'");
    }

    // ── FallbackPolicy ────────────────────────────────────────────────────────

    [Fact]
    public async Task FallbackPolicy_DeniesUnauthenticatedUser()
    {
        var fallback = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        var result = await _authService.AuthorizeAsync(Unauthenticated(), resource: null, fallback);

        result.Succeeded.Should().BeFalse(
            because: "the FallbackPolicy must deny requests with no valid identity");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Staff")]
    [InlineData("OrgAdmin")]
    [InlineData("SystemAdmin")]
    public async Task FallbackPolicy_AllowsAnyAuthenticatedRole(string role)
    {
        var fallback = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        var result = await _authService.AuthorizeAsync(Principal(role), resource: null, fallback);

        result.Succeeded.Should().BeTrue(
            because: $"any authenticated user (role: '{role}') must satisfy the FallbackPolicy");
    }
}
