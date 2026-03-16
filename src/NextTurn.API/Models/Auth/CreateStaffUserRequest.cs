namespace NextTurn.API.Models.Auth;

public sealed record CreateStaffUserRequest(
    string Name,
    string Email,
    string? Phone,
    string Password);
