using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextTurn.Domain.Auth;
using NextTurn.Infrastructure.Persistence;
using Respawn;
using Testcontainers.MsSql;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.Domain.Auth.ValueObjects;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.IntegrationTests;

/// <summary>
/// Shared test fixture: spins up a real SQL Server container, creates the schema,
/// and provides a Respawn-backed reset so each test starts from a clean database.
///
/// Lifecycle (xUnit IClassFixture pattern):
///   - InitializeAsync: start container → build host → create schema → init Respawner
///   - Tests share this single factory instance (cheap: one container per class)
///   - ResetDatabaseAsync: called by each test class before its test methods via IAsyncLifetime
///   - DisposeAsync: stop container
///
/// Why EnsureCreatedAsync instead of MigrateAsync?
///   Tests use EnsureCreatedAsync to create the schema directly from the EF Core model,
///   bypassing the migration history table. This means tests always run against the
///   current model shape — add a column in configuration and it's immediately reflected
///   in tests without running a migration. MigrateAsync is used only for the real
///   development and production databases.
/// </summary>
public sealed class NextTurnWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private Respawner _respawner = null!;

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        // 1. Start the SQL Server container. GetConnectionString() is not valid until after this.
        await _sqlContainer.StartAsync();

        // 2. Trigger host build + create the database schema.
        //    Services getter builds the host, which calls ConfigureWebHost below,
        //    which injects the container's connection string into the DI configuration.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        // 3. Initialise Respawner against the now-populated database.
        //    Respawner snapshots the current tables so it can DELETE their rows efficiently.
        await using var connection = new SqlConnection(_sqlContainer.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer
        });
    }

    // Called by test classes before each test to wipe all rows (not schema).
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(_sqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override the connection string so the app talks to the test container,
        // not any real or local SQL Server instance.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString(),
                // The Testing environment does not load appsettings.Development.json.
                // Inject a valid JWT secret so the JwtBearer handler can initialise its
                // SymmetricSecurityKey (requires at least 16 characters / 128-bit key).
                // This value is test-only — it never leaves the test process.
                ["JwtSettings:Secret"]        = "integration-test-secret-32-chars!!",
                ["JwtSettings:Issuer"]        = "https://localhost:5001",
                ["JwtSettings:Audience"]      = "NextTurnClient",
                ["JwtSettings:ExpiryMinutes"] = "60"
            });
        });

        // Use a dedicated test environment to suppress production-only behaviour.
        builder.UseEnvironment("Testing");
    }

    // ── Token factory ─────────────────────────────────────────────────────────

    /// <summary>
    /// Mints a signed JWT for the given role without going through the login endpoint.
    /// Uses the same secret / issuer / audience injected by ConfigureWebHost so the
    /// API's JwtBearer handler will accept the token as valid.
    ///
    /// Call this in integration tests that need a pre-authenticated client:
    ///   _client.DefaultRequestHeaders.Authorization =
    ///       new AuthenticationHeaderValue("Bearer", _factory.CreateTokenForRole(UserRole.Staff));
    /// </summary>
    public string CreateTokenForRole(
        UserRole role,
        Guid?    userId   = null,
        Guid?    tenantId = null,
        int      expiryMinutes = 60)
    {
        const string secret   = "integration-test-secret-32-chars!!";
        const string issuer   = "https://localhost:5001";
        const string audience = "NextTurnClient";

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // For already-expired tokens (expiryMinutes < 0), both notBefore and expires
        // must be in the past. notBefore is set one minute before expires to keep the
        // JwtSecurityToken constructor happy (expires must be after notBefore).
        var expires   = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var notBefore = expiryMinutes < 0
            ? expires.AddMinutes(-1)
            : DateTime.UtcNow;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   (userId   ?? Guid.NewGuid()).ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"test-{role.ToString().ToLower()}@example.com"),
            new Claim(JwtRegisteredClaimNames.Name,  $"Test {role}"),
            new Claim("role",                        role.ToString()),
            new Claim("tid",                         (tenantId ?? Guid.NewGuid()).ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          notBefore,
            expires:            expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Convenience overload: creates an <see cref="HttpClient"/> pre-configured with
    /// a Bearer token for the given role.
    /// </summary>
    public HttpClient CreateClientForRole(UserRole role, Guid? tenantId = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", CreateTokenForRole(role, tenantId: tenantId));
        return client;
    }

    // ── Test data helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a test organisation and an active queue into the test database.
    /// Returns the IDs so callers can build requests and JWT claims against them.
    ///
    /// Call this inside a test's <c>InitializeAsync</c> (after <c>ResetDatabaseAsync</c>)
    /// to ensure the queue exists before the test runs.
    ///
    /// Example usage in an integration test class:
    /// <code>
    /// public async Task InitializeAsync()
    /// {
    ///     await _factory.ResetDatabaseAsync();
    ///     (_tenantId, _queueId) = await _factory.SeedQueueAsync();
    /// }
    /// </code>
    /// </summary>
    /// <param name="maxCapacity">Override to test queue-full scenarios (e.g. set to 1).</param>
    /// <param name="avgServiceTimeSecs">Seconds per customer — drives ETA in results.</param>
    public async Task<(Guid TenantId, Guid QueueId)> SeedQueueAsync(
        int maxCapacity       = 50,
        int avgServiceTimeSecs = 300)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // ── Organisation (serves as tenant) ───────────────────────────────────
        // Use a unique name per call — there is a UNIQUE index on Organisation.Name,
        // so calling SeedQueueAsync more than once per test would collide if the name
        // were fixed (e.g. in queue-full tests that seed a second, capacity=1 queue).
        var org = OrganisationEntity.Create(
            name:       $"Test Organisation {Guid.NewGuid()}",
            address:    new Address("1 Test Street", "Test City", "T1 1ST", "GB"),
            type:       OrganisationType.Government,
            adminEmail: new EmailAddress("admin@test.nextturn.dev"));

        db.Organisations.Add(org);
        await db.SaveChangesAsync();

        var tenantId = org.Id;

        // ── Queue ─────────────────────────────────────────────────────────────
        var queue = QueueEntity.Create(
            organisationId:            tenantId,
            name:                      "Test Queue",
            maxCapacity:               maxCapacity,
            averageServiceTimeSeconds: avgServiceTimeSecs);

        db.Queues.Add(queue);
        await db.SaveChangesAsync();

        return (tenantId, queue.Id);
    }
}
