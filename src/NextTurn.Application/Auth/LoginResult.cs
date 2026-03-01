namespace NextTurn.Application.Auth;

/// <summary>
/// Returned by LoginUserCommandHandler on successful authentication.
///
/// This record is the payload the API controller serialises into the HTTP 200 response.
/// It contains only the information the client needs to bootstrap a session — no
/// sensitive fields (password hash, failed attempt count, lockout timestamps).
///
/// AccessToken — signed JWT, valid for JwtSettings.ExpiryMinutes (default 60 min).
///               The client stores this in localStorage and sends it as
///               "Authorization: Bearer {token}" on subsequent requests.
/// Role        — UserRole enum name (e.g. "User", "Staff") — duplicated here from
///               the JWT payload so the frontend can branch its UI without decoding
///               the token on every render.
/// </summary>
public sealed record LoginResult(
    string AccessToken,
    Guid UserId,
    string Name,
    string Email,
    string Role
);
