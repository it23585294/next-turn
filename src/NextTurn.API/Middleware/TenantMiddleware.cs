using System.Security.Claims;
using System.Text.Json;
using NextTurn.Infrastructure.Common;

namespace NextTurn.API.Middleware;

/// <summary>
/// Resolves the current request's TenantId early in the pipeline and stores it
/// in HttpContext.Items so downstream services (via HttpTenantContext) can read it.
///
/// Resolution order (first match wins):
///   1. JWT claim "tid"    — authenticated users whose token contains the tenant Guid
///   2. X-Tenant-Id header — unauthenticated requests (e.g. user registration, login)
///
/// Returns 400 Bad Request if neither source provides a valid Guid.
///
/// Why a custom header for unauthenticated requests?
///   During registration, the caller doesn't have a JWT yet. They must tell the API
///   which organisation they belong to. The X-Tenant-Id header is a well-understood
///   REST convention and is validated here before reaching any handler.
///
/// Placement in the pipeline (Program.cs):
///   app.UseAuthentication()   — populates User.Claims from JWT
///   app.UseMiddleware&lt;TenantMiddleware&gt;()  — reads "tid" claim or header
///   app.UseAuthorization()
/// </summary>
public sealed class TenantMiddleware
{
    /// <summary>JWT claim type that carries the tenant Guid.</summary>
    public const string TenantIdClaim = "tid";

    /// <summary>HTTP request header that carries the tenant Guid for unauthenticated requests.</summary>
    public const string TenantIdHeader = "X-Tenant-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var claimTenantId = TryResolveFromClaims(context.User);
        var headerTenantId = TryResolveFromHeader(context.Request);
        var role = context.User.FindFirstValue("role");

        Guid? tenantId;

        // Staff and org admins are strictly scoped to the tenant in their JWT.
        if (context.User.Identity?.IsAuthenticated == true && (role == "Staff" || role == "OrgAdmin"))
        {
            if (claimTenantId == null || claimTenantId == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                var error = new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    title = "Bad Request",
                    status = 400,
                    detail = "Authenticated staff/admin tokens must include a valid tenant claim."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                return;
            }

            if (headerTenantId != null && headerTenantId != claimTenantId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var error = new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    title = "Forbidden",
                    status = 403,
                    detail = "Staff and org admin users can only access resources in their own organisation."
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                return;
            }

            tenantId = claimTenantId;
        }
        else
        {
            // For consumer users and unauthenticated flows, header-first behaviour is retained.
            tenantId = headerTenantId ?? claimTenantId;
        }

        // ── 3. Reject if unresolved ───────────────────────────────────────────
        if (tenantId == null)
        {
            // If the user has no valid identity (no token / invalid token), pass the
            // request through so UseAuthorization() can return the correct 401.
            // Returning 400 here would mask the authentication failure.
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            // Authenticated user with a token that lacks a 'tid' claim — that means
            // a malformed token slipped through, which is a bad request (not a 401).
            _logger.LogWarning(
                "Request to {Method} {Path} rejected: TenantId could not be resolved " +
                "from JWT claim '{Claim}' or header '{Header}'.",
                context.Request.Method,
                context.Request.Path,
                TenantIdClaim,
                TenantIdHeader);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var error = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Bad Request",
                status = 400,
                detail = $"A valid tenant identifier must be provided via the '{TenantIdHeader}' header or a JWT '{TenantIdClaim}' claim."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            return;
        }

        // ── 4. Store in Items for HttpTenantContext ───────────────────────────
        context.Items[TenantContextKeys.TenantId] = tenantId.Value;

        await _next(context);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static Guid? TryResolveFromClaims(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(TenantIdClaim);
        if (claim is null) return null;

        return Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private static Guid? TryResolveFromHeader(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(TenantIdHeader, out var headerValue))
            return null;

        return Guid.TryParse(headerValue, out var id) ? id : null;
    }
}
