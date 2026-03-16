using System.Security.Cryptography;
using System.Text;
using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.AcceptStaffInvite;

public sealed class AcceptStaffInviteCommandHandler : IRequestHandler<AcceptStaffInviteCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AcceptStaffInviteCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Unit> Handle(AcceptStaffInviteCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeSha256(request.Token);
        var staffUser = await _userRepository.GetByStaffInviteTokenHashAsync(tokenHash, cancellationToken);

        if (staffUser is null || !staffUser.HasActiveInviteToken(tokenHash))
            throw new DomainException("Invite token is invalid or expired.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        staffUser.AcceptStaffInvite(passwordHash);

        await _userRepository.UpdateAsync(staffUser, cancellationToken);
        return Unit.Value;
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
