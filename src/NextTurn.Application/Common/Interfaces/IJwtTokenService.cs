using NextTurn.Domain.Auth.Entities;

namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Generates signed JWT access tokens for authenticated users.
///
/// The interface lives in Application so handlers can depend on it without
/// referencing Infrastructure. The concrete implementation (JwtTokenService)
/// lives in Infrastructure and is registered via AddInfrastructure().
///
/// Token claims issued:
///   sub  — user ID (Guid, as string)
///   email
///   name
///   role — UserRole enum name (e.g. "User", "Staff")
///   tid  — tenant ID (Guid, as string) — used by TenantMiddleware on subsequent requests
///   exp  — expiry, controlled by JwtSettings.ExpiryMinutes (default 60)
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT string for the given user and tenant.
    /// </summary>
    /// <param name="user">The authenticated user entity.</param>
    /// <param name="tenantId">The tenant the user is scoped to for this session.</param>
    /// <returns>A signed, compact JWT string.</returns>
    string GenerateToken(User user, Guid tenantId);
}
