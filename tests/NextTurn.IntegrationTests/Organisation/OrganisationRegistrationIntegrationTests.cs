using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Infrastructure.Persistence;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.IntegrationTests.Organisation;

/// <summary>
/// Integration tests for POST /api/organisations.
///
/// The endpoint is [AllowAnonymous] — no token or X-Tenant-Id header is required.
/// Each test starts from a clean database (Respawn wipes rows in InitializeAsync).
///
/// Covered scenarios:
///   1.  Happy path — valid payload → 201 + body contains organisationId + adminUserId
///   2.  Valid payload → Organisations row persisted with Status = PendingApproval
///   3.  Valid payload → Users row persisted with Role = OrgAdmin + TenantId matches OrganisationId
///   4.  Missing OrgName → 422 Unprocessable Entity (FluentValidation)
///   5.  Invalid AdminEmail format → 422 Unprocessable Entity (FluentValidation)
///   6.  Invalid OrgType value → 422 Unprocessable Entity (FluentValidation)
///   7.  Duplicate OrgName (two identical requests) → second call returns 409 Conflict
/// </summary>
[Collection("Integration")]
public sealed class OrganisationRegistrationIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrganisationRegistrationIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
        // Endpoint is [AllowAnonymous] — plain unauthenticated client is correct.
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── 1. Happy path — status code + response body ───────────────────────────

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_Returns201Created()
    {
        var response = await PostAsync(ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_ResponseBodyContainsOrganisationId()
    {
        var response = await PostAsync(ValidPayload());
        var body = await ReadBodyAsync(response);

        body.Should().ContainKey("organisationId");
        var id = body["organisationId"].GetGuid();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_ResponseBodyContainsAdminUserId()
    {
        var response = await PostAsync(ValidPayload());
        var body = await ReadBodyAsync(response);

        body.Should().ContainKey("adminUserId");
        var id = body["adminUserId"].GetGuid();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_LocationHeaderPointsToOrganisation()
    {
        var response = await PostAsync(ValidPayload());
        var body = await ReadBodyAsync(response);

        var organisationId = body["organisationId"].GetGuid();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString()
            .Should().Be($"/api/organisations/{organisationId}");
    }

    // ── 2. Persistence — Organisations table ──────────────────────────────────

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_PersistsOrganisationRow()
    {
        await PostAsync(ValidPayload());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var org = await db.Organisations
            .FirstOrDefaultAsync(o => o.Name == ValidPayload().OrgName);

        org.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_OrganisationStatusIsPendingApproval()
    {
        await PostAsync(ValidPayload());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var org = await db.Organisations
            .FirstAsync(o => o.Name == ValidPayload().OrgName);

        org.Status.Should().Be(OrganisationStatus.PendingApproval);
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_OrganisationHasCorrectAdminEmail()
    {
        var payload = ValidPayload();
        await PostAsync(payload);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var org = await db.Organisations
            .FirstAsync(o => o.Name == payload.OrgName);

        org.AdminEmail.Value.Should().Be(payload.AdminEmail);
    }

    // ── 3. Persistence — Users table (OrgAdmin) ───────────────────────────────

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_PersistsOrgAdminUser()
    {
        var response = await PostAsync(ValidPayload());
        var body = await ReadBodyAsync(response);
        var adminUserId = body["adminUserId"].GetGuid();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // IgnoreQueryFilters bypasses the TenantId global query filter, which would
        // otherwise call HttpTenantContext.TenantId and throw outside an HTTP request.
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == adminUserId);

        user.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_AdminUserHasOrgAdminRole()
    {
        var response = await PostAsync(ValidPayload());
        var body = await ReadBodyAsync(response);
        var adminUserId = body["adminUserId"].GetGuid();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == adminUserId);

        user.Role.Should().Be(NextTurn.Domain.Auth.UserRole.OrgAdmin);
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_AdminUserTenantIdMatchesOrganisationId()
    {
        var response = await PostAsync(ValidPayload());
        var body = await ReadBodyAsync(response);
        var organisationId = body["organisationId"].GetGuid();
        var adminUserId    = body["adminUserId"].GetGuid();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == adminUserId);

        user.TenantId.Should().Be(organisationId);
    }

    [Fact]
    public async Task RegisterOrganisation_WithValidPayload_AdminUserEmailMatchesPayload()
    {
        var payload  = ValidPayload();
        var response = await PostAsync(payload);
        var body     = await ReadBodyAsync(response);
        var adminUserId = body["adminUserId"].GetGuid();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == adminUserId);

        user.Email.Value.Should().Be(payload.AdminEmail);
    }

    // ── 4. Validation failure — missing OrgName ───────────────────────────────

    [Fact]
    public async Task RegisterOrganisation_WithMissingOrgName_Returns422()
    {
        var response = await PostAsync(ValidPayload() with { OrgName = string.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RegisterOrganisation_WithMissingOrgName_ResponseContainsValidationErrors()
    {
        var response = await PostAsync(ValidPayload() with { OrgName = string.Empty });
        var body = await ReadBodyAsync(response);

        body.Should().ContainKey("errors");
    }

    // ── 5. Validation failure — invalid AdminEmail ────────────────────────────

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing-at")]
    public async Task RegisterOrganisation_WithInvalidAdminEmail_Returns422(string badEmail)
    {
        var response = await PostAsync(ValidPayload() with { AdminEmail = badEmail });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RegisterOrganisation_WithInvalidAdminEmail_ResponseContainsEmailError()
    {
        var response = await PostAsync(ValidPayload() with { AdminEmail = "notanemail" });
        var body = await ReadBodyAsync(response);

        body.Should().ContainKey("errors");
        var errors = body["errors"].ToString();
        errors.Should().Contain("AdminEmail");
    }

    // ── 6. Validation failure — invalid OrgType ───────────────────────────────

    [Fact]
    public async Task RegisterOrganisation_WithInvalidOrgType_Returns422()
    {
        var response = await PostAsync(ValidPayload() with { OrgType = "InvalidType" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RegisterOrganisation_WithInvalidOrgType_ResponseContainsOrgTypeError()
    {
        var response = await PostAsync(ValidPayload() with { OrgType = "InvalidType" });
        var body = await ReadBodyAsync(response);

        body.Should().ContainKey("errors");
        var errors = body["errors"].ToString();
        errors.Should().Contain("OrgType");
    }

    // ── 7. Duplicate OrgName → 409 Conflict ──────────────────────────────────

    [Fact]
    public async Task RegisterOrganisation_DuplicateOrgName_SecondRequestReturns409()
    {
        var first  = await PostAsync(ValidPayload());
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await PostAsync(ValidPayload());
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RegisterOrganisation_DuplicateOrgName_ResponseContainsDuplicateDetail()
    {
        await PostAsync(ValidPayload());

        var response = await PostAsync(ValidPayload());
        var body = await ReadBodyAsync(response);

        body.Should().ContainKey("detail");
        body["detail"].ToString().Should().Contain("already registered");
    }

    [Fact]
    public async Task RegisterOrganisation_DuplicateOrgName_OnlyOneOrganisationRowIsStored()
    {
        await PostAsync(ValidPayload());
        await PostAsync(ValidPayload()); // second call should fail silently at HTTP layer

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var count = await db.Organisations
            .CountAsync(o => o.Name == ValidPayload().OrgName);

        count.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<HttpResponseMessage> PostAsync(OrgRegistrationPayload payload) =>
        _client.PostAsJsonAsync("/api/organisations", payload);

    private static async Task<Dictionary<string, JsonElement>> ReadBodyAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static OrgRegistrationPayload ValidPayload(
        string orgName    = "Acme Healthcare Ltd",
        string adminEmail = "admin@acme.com") =>
        new(
            OrgName:      orgName,
            AddressLine1: "123 Main Street",
            City:         "London",
            PostalCode:   "SW1A 1AA",
            Country:      "UK",
            OrgType:      "Healthcare",
            AdminName:    "Jane Smith",
            AdminEmail:   adminEmail);

    /// <summary>Mirror of RegisterOrganisationRequest — avoids importing the API layer model.</summary>
    private sealed record OrgRegistrationPayload(
        string OrgName,
        string AddressLine1,
        string City,
        string PostalCode,
        string Country,
        string OrgType,
        string AdminName,
        string AdminEmail);
}
