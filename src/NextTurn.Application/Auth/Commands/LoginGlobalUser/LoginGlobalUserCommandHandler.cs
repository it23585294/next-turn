using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Application.Auth.Commands.LoginUser;

namespace NextTurn.Application.Auth.Commands.LoginGlobalUser;

/// <summary>
/// Handles LoginGlobalUserCommand — authenticates a consumer (global) user.
///
/// Key differences from LoginUserCommandHandler:
///   - Does NOT inject ITenantContext; there is no tenant for consumer accounts.
///   - Uses GetByEmailGlobalAsync (IgnoreQueryFilters) to find the user across all tenants.
///   - Generates a JWT with TenantId = Guid.Empty, which the frontend can use when
///     calling org-specific endpoints by supplying X-Tenant-Id in request headers.
/// </summary>
public class LoginGlobalUserCommandHandler : IRequestHandler<LoginGlobalUserCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPublisher _publisher;

    public LoginGlobalUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IPublisher publisher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _publisher = publisher;
    }

    public async Task<LoginResult> Handle(LoginGlobalUserCommand command, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(command.Email);

        // Search across ALL tenants — consumer users have TenantId = Guid.Empty
        // and would not be found by the tenant-scoped GetByEmailAsync.
        var user = await _userRepository.GetByEmailGlobalAsync(email, cancellationToken);
        if (user is null)
            throw new DomainException("Invalid credentials.");

        if (user.IsLockedOut())
            throw new DomainException("Account is temporarily locked. Please try again later.");

        bool passwordCorrect = _passwordHasher.Verify(command.Password, user.PasswordHash);
        if (!passwordCorrect)
        {
            user.RecordFailedLogin();

            if (user.IsLockedOut())
            {
                await _publisher.Publish(
                    new AccountLockedNotification(user.Id, user.Email.Value, user.LockoutUntil!.Value),
                    cancellationToken);
            }

            await _userRepository.UpdateAsync(user, cancellationToken);

            throw new DomainException("Invalid credentials.");
        }

        user.Unlock();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // JWT tid claim will be Guid.Empty; the frontend passes X-Tenant-Id per-request
        // for any org-specific API calls (e.g. joining a queue).
        string token = _jwtTokenService.GenerateToken(user, user.TenantId);

        return new LoginResult(
            AccessToken: token,
            UserId: user.Id,
            Name: user.Name,
            Email: user.Email.Value,
            Role: user.Role.ToString()
        );
    }
}
