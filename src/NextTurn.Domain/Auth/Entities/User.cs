using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Domain.Auth.Entities;

public class User {

  public Guid Id { get; }                           // would generate a new uuid (eg: 3f2504e0-4f89-11d3-9a0c-0305e82c3301)
  public string Name { get; private set; }          // only code inside this class can write this
  public EmailAddress Email { get; private set; }
  public string? Phone { get; private set; }        // this is nullable, meaning it could be empty and no errors would be thrown (we have enforced nullability)
  public DateTimeOffset CreatedAt { get; }          // considers utc diff as well (otherwise it would break in other countries)
  public string PasswordHash { get; private set; }
  public bool IsActive { get; private set; }

  private User(Guid Id, string Name, EmailAddress Email, string? Phone, DateTimeOffset CreatedAt, string PasswordHash, bool IsActive) {
    this.Id = Id;
    this.Name = Name;
    this.Phone = Phone;
    this.Email = Email;
    this.CreatedAt = CreatedAt;
    this.PasswordHash = PasswordHash;
    this.IsActive = IsActive;
  }

  public static User Create(string name, EmailAddress email, string? phone, string passwordHash) {
    // validation
    if(string.IsNullOrWhiteSpace(name)) {
      throw new DomainException("Name is required.");
    }
    else if(string.IsNullOrWhiteSpace(passwordHash)) {
      throw new DomainException("Password cannot be empty.");
    }

    Guid id = Guid.NewGuid();
    DateTimeOffset createdAt = DateTimeOffset.UtcNow;

    return new User(id, name, email, phone, createdAt, passwordHash, true);
  }

  public void Activate() {
    IsActive = true;
  }

  public void Deactivate() {
    IsActive = false;
  }
  
}
