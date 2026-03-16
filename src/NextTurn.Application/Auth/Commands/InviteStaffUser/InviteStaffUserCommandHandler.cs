using System.Security.Cryptography;
using System.Text;
using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Auth.Commands.InviteStaffUser;

public sealed class InviteStaffUserCommandHandler : IRequestHandler<InviteStaffUserCommand, InviteStaffUserResult>
{
    private static readonly TimeSpan InviteTtl = TimeSpan.FromDays(2);

    private readonly IUserRepository _userRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IEmailService _emailService;

    public InviteStaffUserCommandHandler(
        IUserRepository userRepository,
        ITenantContext tenantContext,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _tenantContext = tenantContext;
        _emailService = emailService;
    }

    public async Task<InviteStaffUserResult> Handle(InviteStaffUserCommand request, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        User staffUser;

        if (existing is not null)
        {
            if (existing.Role != UserRole.Staff)
                throw new DomainException("A non-staff account already uses this email.");

            staffUser = existing;
        }
        else
        {
            var bootstrapPasswordHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            staffUser = User.Create(
                _tenantContext.TenantId,
                request.Name,
                email,
                request.Phone,
                bootstrapPasswordHash,
                UserRole.Staff);
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var tokenHash = ComputeSha256(rawToken);
        var expiresAt = DateTimeOffset.UtcNow.Add(InviteTtl);

        staffUser.StartStaffInvite(tokenHash, expiresAt);

        if (existing is null)
        {
            await _userRepository.AddAsync(staffUser, cancellationToken);
        }
        else
        {
            await _userRepository.UpdateAsync(staffUser, cancellationToken);
        }

        var invitePath = $"/staff/invite/accept?token={rawToken}";

        await _emailService.SendStaffInviteEmailAsync(
            toEmail: request.Email,
            staffName: request.Name,
            invitePath: invitePath,
            expiresAt: expiresAt,
            cancellationToken: cancellationToken);

        return new InviteStaffUserResult(staffUser.Id, invitePath, expiresAt);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
