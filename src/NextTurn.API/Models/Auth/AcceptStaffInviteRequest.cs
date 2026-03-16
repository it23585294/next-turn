namespace NextTurn.API.Models.Auth;

public sealed record AcceptStaffInviteRequest(
    string Token,
    string Password);
