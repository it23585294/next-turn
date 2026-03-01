using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Testcontainers.MsSql;
using NextTurn.Infrastructure.Persistence;

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
}
