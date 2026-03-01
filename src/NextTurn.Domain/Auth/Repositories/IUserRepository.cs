namespace NextTurn.Domain.Auth.Repositories;

using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;

public interface IUserRepository {
  Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken);     // finds users by email during login
  Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken);          // check if email is taken before registration
  Task AddAsync(User user, CancellationToken cancellationToken);                            // saving the registered user
}
