using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using AppointmentEntity  = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentProfile = NextTurn.Domain.Appointment.Entities.AppointmentProfile;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Infrastructure.Persistence.Configurations.Auth;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using QueueEntity        = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry         = NextTurn.Domain.Queue.Entities.QueueEntry;

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

    public DbSet<User>             Users        => Set<User>();
    public DbSet<OrganisationEntity> Organisations => Set<OrganisationEntity>();

    // Queue module (NT-16) — EF Core entity configurations and migration added in NT-16-3.
    public DbSet<QueueEntity> Queues       => Set<QueueEntity>();
    public DbSet<QueueEntry>  QueueEntries => Set<QueueEntry>();

    // Appointment module (NT-19).
    public DbSet<AppointmentEntity> Appointments => Set<AppointmentEntity>();
    public DbSet<AppointmentProfile> AppointmentProfiles => Set<AppointmentProfile>();
    public DbSet<AppointmentScheduleRule> AppointmentScheduleRules => Set<AppointmentScheduleRule>();

    // ── Model configuration ───────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes in this assembly automatically.
        // Adding a new configuration file is enough — no need to register it here manually.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ── Global Query Filters (multi-tenancy) ─────────────────────────────
        // Every query on a tenant-aware entity automatically appends a tenant WHERE clause.
        // This is enforced at the EF Core level — forgetting to filter in a repository
        // is not possible because the filter is always active.

        // Users: direct TenantId column on the entity.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);

        // Queues: OrganisationId IS the tenant identifier for this module.
        // Every queue belongs to exactly one organisation (= one tenant).
        modelBuilder.Entity<QueueEntity>()
            .HasQueryFilter(q => q.OrganisationId == _tenantContext.TenantId);

        // QueueEntries: no TenantId column — isolation is enforced via a correlated
        // subquery through Queue.OrganisationId. This is an EF Core supported pattern
        // for filtering child entities that don't carry their own tenant key.
        // Set<QueueEntity>() resolves the Queue DbSet (with its own filter disabled here
        // to avoid double-filtering) using IgnoreQueryFilters is not needed — EF Core
        // does not recursively apply filters inside filter lambdas.
        modelBuilder.Entity<QueueEntry>()
            .HasQueryFilter(e =>
                Set<QueueEntity>().Any(q =>
                    q.Id == e.QueueId &&
                    q.OrganisationId == _tenantContext.TenantId));

        // Appointments: scoped by organisation (tenant).
        modelBuilder.Entity<AppointmentEntity>()
            .HasQueryFilter(a => a.OrganisationId == _tenantContext.TenantId);

        modelBuilder.Entity<AppointmentProfile>()
            .HasQueryFilter(p => p.OrganisationId == _tenantContext.TenantId);

        modelBuilder.Entity<AppointmentScheduleRule>()
            .HasQueryFilter(r => r.OrganisationId == _tenantContext.TenantId);
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
                //
                // Global consumer users intentionally use TenantId = Guid.Empty and do
                // not require tenant context on registration. If no tenant is resolved
                // for this request, keep Guid.Empty as-is.
                try
                {
                    entry.Property(nameof(User.TenantId)).CurrentValue = _tenantContext.TenantId;
                }
                catch (Domain.Common.DomainException)
                {
                    // No tenant context available (e.g. register-global) — keep Guid.Empty.
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
