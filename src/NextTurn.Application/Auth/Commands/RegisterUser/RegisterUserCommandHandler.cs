using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.RegisterUser;

/// <summary>
/// Handles the RegisterUserCommand — orchestrates the full registration flow.
///
/// Steps (input validation runs automatically via ValidationBehavior pipeline):
///   1. Construct EmailAddress value object (DomainException if invalid format)
///   2. Check email uniqueness (DomainException if already in use)
///   3. Hash the plaintext password
///   4. Create the User entity via the factory method
///   5. Persist the user via IUserRepository
///   6. Publish UserRegisteredNotification in-process (e.g. to trigger welcome email)
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublisher _publisher;
    private readonly ITenantContext _tenantContext;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPublisher publisher,
        ITenantContext tenantContext)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _publisher = publisher;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // Note: input validation (password complexity, email format, etc.) is handled
        // automatically by ValidationBehavior in the MediatR pipeline — it runs
        // RegisterUserValidator before this method is ever invoked.

        // Step 1 — construct the EmailAddress value object
        // DomainException is thrown here if the format is invalid (secondary check after FluentValidation)
        var email = new EmailAddress(command.Email);

        // Step 2 — enforce uniqueness — reject duplicate emails
        bool emailTaken = await _userRepository.ExistsAsync(email, cancellationToken);

        if (emailTaken)
        {
            throw new DomainException("Email address is already in use.");
        }

        // Step 3 — hash the plaintext password before it ever touches the database
        string passwordHash = _passwordHasher.Hash(command.Password);

        // Step 4 — create the User entity via the factory method
        // Id, CreatedAt, and IsActive are set internally by User.Create()
        // TenantId comes from the current request's JWT claim via ITenantContext
        User user = User.Create(_tenantContext.TenantId, command.Name, email, command.Phone, passwordHash);

        // Step 5 — persist to the database via the repository abstraction
        await _userRepository.AddAsync(user, cancellationToken);

        // Step 6 — publish in-process notification so other modules can react
        // (e.g. NotificationModule sends a welcome email)
        await _publisher.Publish(
            new UserRegisteredNotification(user.Id, user.Email.Value),
            cancellationToken);

        return Unit.Value;
    }
}
