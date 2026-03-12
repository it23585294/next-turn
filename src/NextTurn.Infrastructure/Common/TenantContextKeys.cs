namespace NextTurn.Infrastructure.Common;

/// <summary>
/// Shared constants for keys used in HttpContext.Items to pass tenant data
/// through the middleware pipeline.
/// Both TenantMiddleware (API) and HttpTenantContext (Infrastructure) reference
/// this class to avoid magic strings.
/// </summary>
public static class TenantContextKeys
{
    /// <summary>Key under which TenantMiddleware stores the resolved TenantId.</summary>
    public const string TenantId = "NextTurn_TenantId";
}
