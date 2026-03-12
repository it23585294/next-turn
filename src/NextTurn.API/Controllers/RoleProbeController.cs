using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NextTurn.API.Controllers;

/// <summary>
/// SCAFFOLDING ONLY — remove or gate behind a feature flag before production.
///
/// Provides four lightweight endpoints whose sole purpose is to verify that the
/// authorization policy matrix is enforced correctly. Each endpoint is guarded by
/// one of the four named policies defined in Program.cs and returns the caller's
/// role so integration tests can confirm both the HTTP status and the identity.
///
/// These endpoints have no business logic and carry no sensitive data.
/// They exist to give integration tests (NT-12-4) a stable, purpose-built surface
/// without coupling authorization tests to domain feature endpoints (Sprint 2+).
/// </summary>
[ApiController]
[Route("api/probe")]
public sealed class RoleProbeController : ControllerBase
{
    /// <summary>
    /// Accessible by any authenticated user (User, Staff, OrgAdmin, SystemAdmin).
    /// </summary>
    [HttpGet("user")]
    [Authorize(Policy = "IsUser")]
    [ProducesResponseType(typeof(ProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult UserProbe()
        => Ok(new ProbeResponse(CallerRole()));

    /// <summary>
    /// Accessible by Staff, OrgAdmin, and SystemAdmin.
    /// </summary>
    [HttpGet("staff")]
    [Authorize(Policy = "IsStaff")]
    [ProducesResponseType(typeof(ProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult StaffProbe()
        => Ok(new ProbeResponse(CallerRole()));

    /// <summary>
    /// Accessible by OrgAdmin and SystemAdmin.
    /// </summary>
    [HttpGet("org-admin")]
    [Authorize(Policy = "IsOrgAdmin")]
    [ProducesResponseType(typeof(ProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult OrgAdminProbe()
        => Ok(new ProbeResponse(CallerRole()));

    /// <summary>
    /// Accessible by SystemAdmin only.
    /// </summary>
    [HttpGet("system-admin")]
    [Authorize(Policy = "IsSystemAdmin")]
    [ProducesResponseType(typeof(ProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult SystemAdminProbe()
        => Ok(new ProbeResponse(CallerRole()));

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string CallerRole()
        => User.FindFirstValue("role") ?? "unknown";
}

/// <summary>Response body returned by all probe endpoints.</summary>
public sealed record ProbeResponse(string Role);
