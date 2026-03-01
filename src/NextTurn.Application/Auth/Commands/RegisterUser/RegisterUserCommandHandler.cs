using FluentValidation;
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
/// Steps:
///   1. Validate input via FluentValidation
///   2. Construct EmailAddress value object (throws DomainException if invalid format)
///   3. Check email uniqueness (throws DomainException if already in use)
///   4. Hash the plaintext password
///   5. Create the User entity via the factory method
///   6. Persist the user via IUserRepository
///   7. Publish UserRegisteredNotification in-process (e.g. to trigger welcome email)
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
        // Step 1 — validate inputs against business rules (password complexity, email format, etc.)
        var validator = new RegisterUserValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Step 2 — construct the EmailAddress value object
        // DomainException is thrown here if the format is invalid (secondary check after FluentValidation)
        var email = new EmailAddress(command.Email);

        // Step 3 — enforce uniqueness — reject duplicate emails
        bool emailTaken = await _userRepository.ExistsAsync(email, cancellationToken);

        if (emailTaken)
        {
            throw new DomainException("Email address is already in use.");
        }

        // Step 4 — hash the plaintext password before it ever touches the database
        string passwordHash = _passwordHasher.Hash(command.Password);

        // Step 5 — create the User entity via the factory method
        // Id, CreatedAt, and IsActive are set internally by User.Create()
        // TenantId comes from the current request's JWT claim via ITenantContext
        User user = User.Create(_tenantContext.TenantId, command.Name, email, command.Phone, passwordHash);

        // Step 6 — persist to the database via the repository abstraction
        await _userRepository.AddAsync(user, cancellationToken);

        // Step 7 — publish in-process notification so other modules can react
        // (e.g. NotificationModule sends a welcome email)
        await _publisher.Publish(
            new UserRegisteredNotification(user.Id, user.Email.Value),
            cancellationToken);

        return Unit.Value;
    }
}
