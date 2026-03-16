using MediatR;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.ReactivateStaffUser;

public sealed class ReactivateStaffUserCommandHandler : IRequestHandler<ReactivateStaffUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public ReactivateStaffUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(ReactivateStaffUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new DomainException("Staff user not found.");

        if (user.Role != UserRole.Staff)
            throw new DomainException("Only staff accounts can be reactivated from this endpoint.");

        if (user.IsActive)
            return Unit.Value;

        user.Activate();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Unit.Value;
    }
}
