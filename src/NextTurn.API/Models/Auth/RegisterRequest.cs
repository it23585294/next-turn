namespace NextTurn.API.Models.Auth;

/// <summary>
/// HTTP request body for POST /api/auth/register.
/// Plain data-transfer object — no logic, no dependencies.
///
/// Intentionally separate from RegisterUserCommand so that:
///   - The API contract (field names, JSON shape) can evolve independently of the command.
///   - The controller remains a thin adapter: map DTO → command → send → return response.
/// </summary>
public sealed record RegisterRequest(
    string Name,
    string Email,
    string? Phone,
    string Password
);
