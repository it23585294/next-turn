using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Infrastructure.Auth;

namespace NextTurn.UnitTests.Infrastructure.Auth;

/// <summary>
/// Unit tests for JwtTokenService.
///
/// These tests exercise the concrete Infrastructure implementation directly —
/// they verify the token is structurally valid and carries the correct claims.
/// No mocking needed: JwtTokenService is a pure function (input → signed JWT string).
///
/// Why test Infrastructure here instead of Application?
///   The UnitTests project references Infrastructure specifically to cover
///   JwtTokenService, which has no IO dependencies — it's deterministic and fast.
///   Infrastructure repositories and DbContext are NOT tested here (covered by
///   integration tests).
/// </summary>
public sealed class JwtTokenServiceTests
{
    // ── Known test configuration ──────────────────────────────────────────────

    private const string Secret    = "test-secret-that-is-at-least-32-chars-long!!";
    private const string Issuer    = "https://test.nextturn.app";
    private const string Audience  = "NextTurnTestClient";
    private const int    Expiry    = 60;

    private static readonly Guid TenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var settings = Options.Create(new JwtSettings
        {
            Secret        = Secret,
            Issuer        = Issuer,
            Audience      = Audience,
            ExpiryMinutes = Expiry,
        });

        _service = new JwtTokenService(settings);
    }

    // ── Token structure ───────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = _service.GenerateToken(BuildUser(), TenantId);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_ReturnsValidThreePartJwt()
    {
        var token = _service.GenerateToken(BuildUser(), TenantId);

        // A compact JWT has exactly 3 base64url segments separated by dots
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_CanBeParsedByJwtSecurityTokenHandler()
    {
        var token = _service.GenerateToken(BuildUser(), TenantId);

        var handler = new JwtSecurityTokenHandler();
        var act = () => handler.ReadJwtToken(token);

        act.Should().NotThrow();
    }

    // ── Claims ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ContainsCorrectSubClaim()
    {
        var user  = BuildUser();
        var token = ParseToken(_service.GenerateToken(user, TenantId));

        token.Subject.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_ContainsCorrectEmailClaim()
    {
        var user  = BuildUser();
        var token = ParseToken(_service.GenerateToken(user, TenantId));

        token.Claims
            .First(c => c.Type == JwtRegisteredClaimNames.Email)
            .Value.Should().Be("alice@example.com");
    }

    [Fact]
    public void GenerateToken_ContainsCorrectNameClaim()
    {
        var user  = BuildUser();
        var token = ParseToken(_service.GenerateToken(user, TenantId));

        token.Claims
            .First(c => c.Type == JwtRegisteredClaimNames.Name)
            .Value.Should().Be("Alice Smith");
    }

    [Fact]
    public void GenerateToken_ContainsRoleAsString_NotInteger()
    {
        // Role MUST be a string (e.g. "User") not an integer (e.g. "0").
        // Integer roles are unreadable in the DB and break if enum order changes.
        var user  = BuildUser(role: UserRole.Staff);
        var token = ParseToken(_service.GenerateToken(user, TenantId));

        token.Claims
            .First(c => c.Type == "role")
            .Value.Should().Be("Staff");
    }

    [Fact]
    public void GenerateToken_ContainsCorrectTenantIdClaim()
    {
        var user  = BuildUser();
        var token = ParseToken(_service.GenerateToken(user, TenantId));

        token.Claims
            .First(c => c.Type == "tid")
            .Value.Should().Be(TenantId.ToString());
    }

    [Fact]
    public void GenerateToken_ContainsJtiClaim()
    {
        var token = ParseToken(_service.GenerateToken(BuildUser(), TenantId));

        // jti (JWT ID) must be present and be a valid Guid
        var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        Guid.TryParse(jti, out _).Should().BeTrue("jti should be a valid Guid");
    }

    [Fact]
    public void GenerateToken_TwoCallsProduceDifferentJtiValues()
    {
        // Each token must have a unique jti — required for future revocation support
        var user   = BuildUser();
        var token1 = ParseToken(_service.GenerateToken(user, TenantId));
        var token2 = ParseToken(_service.GenerateToken(user, TenantId));

        var jti1 = token1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = token2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    // ── Expiry ────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ExpiryIsApproximately60MinutesFromNow()
    {
        // JWT ValidTo is truncated to second precision — strip sub-second from boundary
        var before = DateTime.UtcNow.AddMinutes(Expiry).AddMilliseconds(-999);
        var token  = ParseToken(_service.GenerateToken(BuildUser(), TenantId));
        var after  = DateTime.UtcNow.AddMinutes(Expiry).AddSeconds(1);

        token.ValidTo.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void GenerateToken_IssuerMatchesConfiguration()
    {
        var token = ParseToken(_service.GenerateToken(BuildUser(), TenantId));

        token.Issuer.Should().Be(Issuer);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JwtSecurityToken ParseToken(string raw) =>
        new JwtSecurityTokenHandler().ReadJwtToken(raw);

    private static User BuildUser(UserRole role = UserRole.User) =>
        User.Create(
            tenantId:     Guid.Parse("44444444-4444-4444-4444-444444444444"),
            name:         "Alice Smith",
            email:        new EmailAddress("alice@example.com"),
            phone:        null,
            passwordHash: "$2a$12$hashedvalue",
            role:         role);
}
