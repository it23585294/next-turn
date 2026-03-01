using Microsoft.AspNetCore.Http;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Infrastructure.Common;

namespace NextTurn.Infrastructure.Auth;

/// <summary>
/// Runtime implementation of ITenantContext.
/// Reads the current request's TenantId from HttpContext.Items, where
/// TenantMiddleware has already placed it after resolving from the JWT claim
/// or X-Tenant-Id header.
///
/// Lifetime: Scoped — one instance per HTTP request, matching DbContext lifetime.
///
/// Why HttpContext.Items instead of reading claims directly here?
///   TenantMiddleware is the single authoritative place that validates and resolves
///   the tenant identity. Centralising the logic there means the rules for
///   "how is a tenant determined" live in one place. HttpTenantContext is then
///   a simple, testable reader.
///
/// Why not inject IHttpContextAccessor directly and read claims/headers?
///   It works, but couples this class to both the JWT claim format AND the header
///   convention. Middleware as the single resolver is easier to change later
///   (e.g. subdomain-based tenancy) without touching Infrastructure.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// The resolved TenantId for the current HTTP request.
    /// Throws if accessed outside of an HTTP context or before TenantMiddleware runs.
    /// In normal operation this never throws — TenantMiddleware short-circuits with
    /// 400 Bad Request before any handler that needs TenantId is reached.
    /// </summary>
    public Guid TenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException(
                    "HttpTenantContext was accessed outside of an active HTTP request.");

            if (context.Items.TryGetValue(TenantContextKeys.TenantId, out var raw) &&
                raw is Guid tenantId)
            {
                return tenantId;
            }

            throw new InvalidOperationException(
                "TenantId has not been set. Ensure TenantMiddleware is registered " +
                "in the request pipeline before any endpoint that requires a tenant context.");
        }
    }
}
