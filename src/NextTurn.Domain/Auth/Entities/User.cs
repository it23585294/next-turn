using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Domain.Auth.Entities;

public class User {

  // ── Lockout constants ────────────────────────────────────────────────────
  private const int FailedLoginThreshold = 3;
  private static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(10);

  // ── Identity & tenancy ───────────────────────────────────────────────────
  public Guid Id { get; }
  public Guid TenantId { get; private set; }        // which organisation this user belongs to (multi-tenancy)
  public string Name { get; private set; }
  public EmailAddress Email { get; private set; }
  public string? Phone { get; private set; }
  public DateTimeOffset CreatedAt { get; }
  public string PasswordHash { get; private set; }
  public bool IsActive { get; private set; }

  // ── Role & security ──────────────────────────────────────────────────────
  public UserRole Role { get; private set; }
  public int FailedLoginAttempts { get; private set; }
  public DateTimeOffset? LockoutUntil { get; private set; }

  /// <summary>
  /// Reserved for Sprint 2+ MFA implementation. Always false in Sprint 1.
  /// Stored on the entity now so the column exists in the DB schema before we need it.
  /// </summary>
  public bool MfaEnabled { get; private set; }

  // Required by EF Core for entity materialization (loading from the database).
  // EF Core cannot bind constructor parameters to owned-type navigations (like EmailAddress),
  // so it needs a parameterless constructor. Protected prevents accidental use in domain code.
  // EF Core then sets each property via its backing field / private setter after construction.
  protected User() { }

  private User(Guid id, Guid tenantId, string name, EmailAddress email, string? phone, DateTimeOffset createdAt, string passwordHash, bool isActive, UserRole role)
  {
    Id = id;
    TenantId = tenantId;
    Name = name;
    Email = email;
    Phone = phone;
    CreatedAt = createdAt;
    PasswordHash = passwordHash;
    IsActive = isActive;
    Role = role;
    FailedLoginAttempts = 0;
    LockoutUntil = null;
    MfaEnabled = false;
  }

  public static User Create(Guid tenantId, string name, EmailAddress email, string? phone, string passwordHash, UserRole role = UserRole.User)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException("Name is required.");

    if (string.IsNullOrWhiteSpace(passwordHash))
      throw new DomainException("Password hash is required.");

    return new User(
      id: Guid.NewGuid(),
      tenantId: tenantId,
      name: name,
      email: email,
      phone: phone,
      createdAt: DateTimeOffset.UtcNow,
      passwordHash: passwordHash,
      isActive: true,
      role: role
    );
  }

  public void Activate() {
    IsActive = true;
  }

  public void Deactivate() {
    IsActive = false;
  }

  // ── Login / lockout behaviour ─────────────────────────────────────────────

  /// <summary>
  /// Returns true if the user is currently within an active lockout window.
  /// Checks the wall clock so an expired lockout is transparent without any explicit unlock call.
  /// </summary>
  public bool IsLockedOut() => LockoutUntil.HasValue && LockoutUntil.Value > DateTimeOffset.UtcNow;

  /// <summary>
  /// Called by the login handler on each wrong-password attempt.
  /// Automatically triggers a 10-minute lockout when the failure threshold (3) is reached.
  /// </summary>
  public void RecordFailedLogin()
  {
    FailedLoginAttempts++;
    if (FailedLoginAttempts >= FailedLoginThreshold)
      Lock(DefaultLockoutDuration);
  }

  /// <summary>
  /// Locks the account for the specified duration.
  /// Can also be called externally (e.g. admin action or suspicious-activity detection).
  /// </summary>
  public void Lock(TimeSpan duration)
  {
    LockoutUntil = DateTimeOffset.UtcNow.Add(duration);
  }

  /// <summary>
  /// Resets failed attempt counter and clears any active lockout.
  /// Called by the login handler after a successful authentication.
  /// </summary>
  public void Unlock()
  {
    FailedLoginAttempts = 0;
    LockoutUntil = null;
  }
  
}
