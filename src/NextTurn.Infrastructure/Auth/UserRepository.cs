using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.Infrastructure.Auth;

/// <summary>
/// EF Core implementation of IUserRepository.
///
/// Lives in Infrastructure — the only layer allowed to know about EF Core.
/// The Application layer (handler) depends on IUserRepository, never this class.
///
/// Multi-tenancy note:
///   The Global Query Filter on ApplicationDbContext already appends
///   WHERE TenantId = @currentTenantId to every query on the Users table.
///   This repository does NOT add any extra TenantId filter — EF Core handles it.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(
        EmailAddress email,
        CancellationToken cancellationToken = default)
    {
        // EF Core translates the owned-entity property access (Email.Value)
        // into a column predicate on Email_Value — the column defined in UserConfiguration.
        // The Global Query Filter is applied automatically — only the current tenant's
        // users are searched.
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(
        EmailAddress email,
        CancellationToken cancellationToken = default)
    {
        // AnyAsync generates a more efficient SQL query than FirstOrDefaultAsync:
        //   SELECT CASE WHEN EXISTS (...) THEN 1 ELSE 0 END
        // Used in the registration flow to detect duplicate emails before inserting.
        return await _context.Users
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        // Track the new entity — EF Core will generate an INSERT on SaveChangesAsync.
        await _context.Users.AddAsync(user, cancellationToken);

        // Commit the unit of work for this operation.
        // Each command in this system is its own transaction — no external
        // Unit of Work orchestration is needed at this scope.
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        // Mark all scalar properties as modified — EF Core generates a full UPDATE.
        // Used by the login handler to persist FailedLoginAttempts and LockoutUntil
        // after each authentication attempt.
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
