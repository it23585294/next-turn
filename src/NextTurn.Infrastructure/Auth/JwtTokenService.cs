using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;

namespace NextTurn.Infrastructure.Auth;

/// <summary>
/// Generates signed JWT access tokens for authenticated users.
/// Implements IJwtTokenService defined in the Application layer.
///
/// Algorithm: HMAC-SHA256 (HS256) — symmetric, suitable for a single-service
/// monolith where only our own API needs to validate tokens.
///
/// Claims issued:
///   JwtRegisteredClaimNames.Sub  — user ID (Guid string)
///   JwtRegisteredClaimNames.Email
///   JwtRegisteredClaimNames.Name
///   "role"                       — UserRole enum name (e.g. "User", "Staff")
///   "tid"                        — tenant ID (Guid string), read by TenantMiddleware
///   JwtRegisteredClaimNames.Jti  — unique token ID (new Guid per token, aids revocation later)
///   JwtRegisteredClaimNames.Iat  — issued-at Unix timestamp
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(User user, Guid tenantId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim(JwtRegisteredClaimNames.Name,  user.Name),
            new Claim("role",                        user.Role.ToString()),
            new Claim("tid",                         tenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                      DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                      ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
