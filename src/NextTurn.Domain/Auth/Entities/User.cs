using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Domain.Auth.Entities;

public class User {

  public Guid Id { get; }
  public Guid TenantId { get; private set; }        // which organisation this user belongs to (multi-tenancy)
  public string Name { get; private set; }
  public EmailAddress Email { get; private set; }
  public string? Phone { get; private set; }
  public DateTimeOffset CreatedAt { get; }
  public string PasswordHash { get; private set; }
  public bool IsActive { get; private set; }

  private User(Guid id, Guid tenantId, string name, EmailAddress email, string? phone, DateTimeOffset createdAt, string passwordHash, bool isActive)
  {
    Id = id;
    TenantId = tenantId;
    Name = name;
    Email = email;
    Phone = phone;
    CreatedAt = createdAt;
    PasswordHash = passwordHash;
    IsActive = isActive;
  }

  public static User Create(Guid tenantId, string name, EmailAddress email, string? phone, string passwordHash)
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
      isActive: true
    );
  }

  public void Activate() {
    IsActive = true;
  }

  public void Deactivate() {
    IsActive = false;
  }
  
}
