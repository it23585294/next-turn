using MediatR;
using NextTurn.Application.Auth.Commands.RegisterUser;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.CreateStaffUser;

public sealed class CreateStaffUserCommandHandler : IRequestHandler<CreateStaffUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublisher _publisher;
    private readonly ITenantContext _tenantContext;

    public CreateStaffUserCommandHandler(
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

    public async Task<Unit> Handle(CreateStaffUserCommand command, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(command.Email);

        bool emailTaken = await _userRepository.ExistsAsync(email, cancellationToken);
        if (emailTaken)
            throw new DomainException("Email address is already in use.");

        string passwordHash = _passwordHasher.Hash(command.Password);

        var staffUser = User.Create(
            _tenantContext.TenantId,
            command.Name,
            email,
            command.Phone,
            passwordHash,
            UserRole.Staff);

        await _userRepository.AddAsync(staffUser, cancellationToken);

        await _publisher.Publish(
            new UserRegisteredNotification(staffUser.Id, staffUser.Email.Value),
            cancellationToken);

        return Unit.Value;
    }
}
