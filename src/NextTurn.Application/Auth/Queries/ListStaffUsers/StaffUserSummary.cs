namespace NextTurn.Application.Auth.Queries.ListStaffUsers;

public sealed record StaffUserSummary(
    Guid UserId,
    string Name,
    string Email,
    string? Phone,
    bool IsActive,
    DateTimeOffset CreatedAt);
