namespace NextTurn.Domain.Auth.Repositories;

using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;

public interface IUserRepository {
  Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken);     // finds users by email during login (tenant-scoped)
  Task<User?> GetByEmailGlobalAsync(EmailAddress email, CancellationToken cancellationToken); // finds users by email across ALL tenants (for consumer login)
  Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken);          // check if email is taken before registration (tenant-scoped)
  Task<bool> ExistsGlobalAsync(EmailAddress email, CancellationToken cancellationToken);    // check if email is taken across ALL tenants (for consumer registration)
  Task AddAsync(User user, CancellationToken cancellationToken);                            // saving the registered user
  Task UpdateAsync(User user, CancellationToken cancellationToken);                         // persists lockout state and failed attempt count after login attempts
}
