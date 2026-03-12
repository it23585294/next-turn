namespace NextTurn.Infrastructure.Auth;

/// <summary>
/// Strongly-typed configuration POCO bound to the "JwtSettings" section in appsettings.json.
///
/// Bound in AddInfrastructure() via services.Configure&lt;JwtSettings&gt;(config.GetSection("JwtSettings"))
/// and injected into JwtTokenService via IOptions&lt;JwtSettings&gt;.
///
/// Security note: Secret must be at least 32 characters (256 bits) to satisfy HMAC-SHA256.
/// In production, store the Secret in Azure Key Vault / an environment variable override —
/// never commit a real secret to source control.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Signing secret — used to generate and validate HMAC-SHA256 signatures.
    /// Must be at least 32 characters.
    /// </summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// Value placed in the "iss" (issuer) claim.
    /// Typically the base URL of the API (e.g. "https://api.nextturn.app").
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Value placed in the "aud" (audience) claim.
    /// Typically the client app name or base URL (e.g. "NextTurnClient").
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// How many minutes until the token expires. Default 60 (1 hour).
    /// </summary>
    public int ExpiryMinutes { get; init; } = 60;
}
