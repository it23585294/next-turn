using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Auth.Repositories;

namespace NextTurn.Application.Auth.Commands.LoginUser;

/// <summary>
/// Handles the LoginUserCommand — orchestrates the full authentication flow.
///
/// Input validation (email format, required fields) runs automatically via
/// ValidationBehavior before this handler is ever invoked.
///
/// 7-step flow:
///   1. Construct EmailAddress value object
///   2. Fetch user by email — generic error if not found (no information leak)
///   3. Check current lockout status — specific error if locked
///   4. Verify password — on failure: record attempt, possibly lock, persist, publish notification, generic error
///   5. On success: reset failed attempt counter, persist
///   6. Generate JWT via IJwtTokenService
///   7. Return LoginResult
///
/// Security design decisions:
///   - Steps 2 and 4 both throw "Invalid credentials." — intentionally identical.
///     An attacker cannot distinguish between "email not found" and "wrong password".
///   - Lockout is checked BEFORE password verification so a locked user cannot
///     cycle through passwords to discover the correct one.
///   - UpdateAsync is always called on failed attempts so lockout is durable across
///     server restarts and load-balanced instances.
/// </summary>
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPublisher _publisher;
    private readonly ITenantContext _tenantContext;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IPublisher publisher,
        ITenantContext tenantContext)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _publisher = publisher;
        _tenantContext = tenantContext;
    }

    public async Task<LoginResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        // Step 1 — construct the EmailAddress value object
        var email = new EmailAddress(command.Email);

        // Step 2 — look up the user by email within the current tenant
        // The EF Core global query filter scopes this to the tenant from ITenantContext.
        // Generic error message: never reveal whether the email exists or not.
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
            throw new DomainException("Invalid credentials.");

        // Step 3 — check lockout before attempting password verification
        // This prevents brute-forcing through a locked account by rejecting early.
        if (user.IsLockedOut())
            throw new DomainException("Account is temporarily locked. Please try again later.");

        // Step 4 — verify the submitted password against the stored BCrypt hash
        bool passwordCorrect = _passwordHasher.Verify(command.Password, user.PasswordHash);
        if (!passwordCorrect)
        {
            // Record the failed attempt — this may trigger a lockout if threshold is reached
            user.RecordFailedLogin();

            // If the account just became locked, publish the notification event
            if (user.IsLockedOut())
            {
                await _publisher.Publish(
                    new AccountLockedNotification(user.Id, user.Email.Value, user.LockoutUntil!.Value),
                    cancellationToken);
            }

            // Persist the updated attempt counter / lockout timestamp
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Always the same generic message — no information leakage
            throw new DomainException("Invalid credentials.");
        }

        // Step 5 — successful authentication: reset failed attempt counter
        user.Unlock();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Step 6 — generate signed JWT with user claims + tenant ID
        var tenantId = _tenantContext.TenantId;
        string token = _jwtTokenService.GenerateToken(user, tenantId);

        // Step 7 — return the login result to the controller
        return new LoginResult(
            AccessToken: token,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email.Value,
            Role: user.Role.ToString()
        );
    }
}
