using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NextTurn.Application.Common.Interfaces;

namespace NextTurn.Infrastructure.Persistence;

/// <summary>
/// Used exclusively by the "dotnet ef migrations" CLI tool at design time.
/// The tool can't use the real DI container (no running app), so this factory
/// provides a DbContext instance with a dummy ITenantContext.
///
/// This class is NEVER used at runtime — only during migration generation.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=NextTurnDev;User Id=sa;Password=NextTurn_Dev#2026;TrustServerCertificate=True")
            .Options;

        // Dummy tenant context for design-time — TenantId value doesn't matter
        // since migrations only inspect the model structure, not filter data
        return new ApplicationDbContext(options, new DesignTimeTenantContext());
    }

    /// <summary>
    /// Stub implementation of ITenantContext for design-time migration generation only.
    /// </summary>
    private class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
    }
}
