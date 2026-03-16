namespace NextTurn.API.Models.Auth;

public sealed record InviteStaffUserRequest(
    string Name,
    string Email,
    string? Phone);
