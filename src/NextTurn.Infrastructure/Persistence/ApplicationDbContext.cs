using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Infrastructure.Persistence.Configurations.Auth;

namespace NextTurn.Infrastructure.Persistence;

/// <summary>
/// The single EF Core DbContext for the entire NextTurn application.
/// One context, multiple modules — module boundaries are enforced by code
/// convention, not separate databases.
///
/// Key responsibilities:
///   - Applies all entity configurations (via IEntityTypeConfiguration)
///   - Enforces multi-tenant data isolation via Global Query Filters
///   - Auto-sets TenantId on new entities before saving
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    // ── DbSets (one per aggregate root) ──────────────────────────────────────

    public DbSet<User> Users => Set<User>();

    // ── Model configuration ───────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes in this assembly automatically.
        // Adding a new configuration file is enough — no need to register it here manually.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ── Global Query Filters (multi-tenancy) ─────────────────────────────
        // Every query on this table automatically appends WHERE TenantId = @currentTenantId.
        // This is enforced at the EF Core level — forgetting to filter in a repository
        // is not possible because the filter is always active.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
    }

    // ── SaveChanges override ──────────────────────────────────────────────────

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Before saving, auto-set TenantId on any new entity that doesn't have it set yet.
        // This is a safety net — User.Create() already sets TenantId, but this guards
        // against any entity added without going through the factory method.
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                // Use EF Core's Property API to bypass the private setter on TenantId.
                // EF Core can set any tracked property regardless of C# access modifiers.
                entry.Property(nameof(User.TenantId)).CurrentValue = _tenantContext.TenantId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
