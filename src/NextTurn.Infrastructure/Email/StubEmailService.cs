using Microsoft.Extensions.Logging;
using NextTurn.Application.Common.Interfaces;

namespace NextTurn.Infrastructure.Email;

/// <summary>
/// No-op implementation of <see cref="IEmailService"/> used in Sprint 1.
/// Logs the email details at Information level instead of sending a real email.
/// Replace with an SMTP or SendGrid implementation in a later sprint.
/// </summary>
public sealed class StubEmailService : IEmailService
{
    private readonly ILogger<StubEmailService> _logger;

    public StubEmailService(ILogger<StubEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(
        string toEmail,
        string orgName,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[STUB] Welcome email would be sent to {Email} for organisation '{OrgName}'. " +
            "Temporary password: {TemporaryPassword}",
            toEmail,
            orgName,
            temporaryPassword);

        return Task.CompletedTask;
    }

    public Task SendStaffInviteEmailAsync(
        string toEmail,
        string staffName,
        string invitePath,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[STUB] Staff invite email would be sent to {Email} ({StaffName}). " +
            "Invite link: {InvitePath}. Expires: {ExpiresAt}",
            toEmail,
            staffName,
            invitePath,
            expiresAt);

        return Task.CompletedTask;
    }
}
