namespace NextTurn.Application.Auth.Commands.InviteStaffUser;

public sealed record InviteStaffUserResult(
    Guid UserId,
    string InvitePath,
    DateTimeOffset ExpiresAt);
