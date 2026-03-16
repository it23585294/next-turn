using MediatR;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.DeactivateStaffUser;

public sealed class DeactivateStaffUserCommandHandler : IRequestHandler<DeactivateStaffUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public DeactivateStaffUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(DeactivateStaffUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new DomainException("Staff user not found.");

        if (user.Role != UserRole.Staff)
            throw new DomainException("Only staff accounts can be deactivated from this endpoint.");

        if (!user.IsActive)
            return Unit.Value;

        user.Deactivate();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Unit.Value;
    }
}
