using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.RegisterGlobalUser;

/// <summary>
/// Handles RegisterGlobalUserCommand — registers a consumer (end-user) account.
///
/// Key differences from RegisterUserCommandHandler:
///   - Does NOT inject ITenantContext; consumer users are not bound to any tenant.
///   - Uses ExistsGlobalAsync so email uniqueness is enforced across ALL tenants.
///   - Passes Guid.Empty as TenantId to User.Create().
///
/// The resulting user can join queues from any organisation.
/// </summary>
public class RegisterGlobalUserCommandHandler : IRequestHandler<RegisterGlobalUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublisher _publisher;

    public RegisterGlobalUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPublisher publisher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(RegisterGlobalUserCommand command, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(command.Email);

        // Check uniqueness across ALL tenants — a consumer email can only exist once.
        bool emailTaken = await _userRepository.ExistsGlobalAsync(email, cancellationToken);
        if (emailTaken)
            throw new DomainException("Email address is already in use.");

        string passwordHash = _passwordHasher.Hash(command.Password);

        // Guid.Empty signals "no tenant" — this user is a consumer, not an org member.
        User user = User.Create(Guid.Empty, command.Name, email, command.Phone, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        await _publisher.Publish(
            new RegisterGlobalUserNotification(user.Id, user.Email.Value),
            cancellationToken);

        return Unit.Value;
    }
}
