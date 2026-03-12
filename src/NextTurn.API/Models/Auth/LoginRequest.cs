namespace NextTurn.API.Models.Auth;

/// <summary>
/// Request body for POST /api/auth/login.
/// Intentionally flat — no password complexity rules here (that belongs in the Application layer validator).
/// </summary>
public sealed record LoginRequest(string Email, string Password);
