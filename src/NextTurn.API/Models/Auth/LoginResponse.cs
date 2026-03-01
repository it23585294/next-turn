namespace NextTurn.API.Models.Auth;

/// <summary>
/// HTTP 200 response body for POST /api/auth/login.
///
/// Intentionally a separate type from Application.LoginResult:
///   - The API contract (what crosses the wire) should not be coupled to
///     the application layer's internal return value.
///   - If LoginResult ever gains internal fields, they won't bleed into the response.
///
/// Role is returned as a string (e.g. "User", "Staff") so the frontend can
/// branch its UI without decoding the JWT on every render.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    Guid   UserId,
    string Name,
    string Email,
    string Role
);
