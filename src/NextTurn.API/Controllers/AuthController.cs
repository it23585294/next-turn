using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTurn.API.Models.Auth;
using NextTurn.Application.Auth;
using NextTurn.Application.Auth.Commands.CreateStaffUser;
using NextTurn.Application.Auth.Commands.InviteStaffUser;
using NextTurn.Application.Auth.Commands.AcceptStaffInvite;
using NextTurn.Application.Auth.Commands.DeactivateStaffUser;
using NextTurn.Application.Auth.Commands.LoginGlobalUser;
using NextTurn.Application.Auth.Commands.LoginUser;
using NextTurn.Application.Auth.Commands.ReactivateStaffUser;
using NextTurn.Application.Auth.Commands.RegisterGlobalUser;
using NextTurn.Application.Auth.Commands.RegisterUser;
using NextTurn.Application.Auth.Queries.ListStaffUsers;

namespace NextTurn.API.Controllers;

/// <summary>
/// Handles authentication-related endpoints (register, login, etc.)
///
/// The controller is intentionally thin — it:
///   1. Accepts the HTTP request and maps it to a command/query
///   2. Sends through MediatR
///   3. Maps the result to an HTTP response
///
/// All business logic lives in the Application layer handlers.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Register a new user under the tenant identified by X-Tenant-Id header.
    /// </summary>
    /// <remarks>
    /// The caller must supply an X-Tenant-Id header containing the organisation's
    /// Guid. This is picked up by TenantMiddleware and made available to the
    /// handler via ITenantContext.
    ///
    /// Returns 201 Created on success. The response body is intentionally empty —
    /// no user data is echoed back to avoid leaking internal identifiers at this stage.
    /// A Location header pointing to the created resource will be added in a future
    /// story once the GET /api/users/{id} endpoint exists.
    ///
    /// Error responses:
    ///   400 — domain business rule violated (e.g. email already registered)
    ///   422 — input validation failed (e.g. missing field, weak password)
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Name,
            request.Email,
            request.Phone,
            request.Password);

        await _sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Authenticate an existing user and return a signed JWT.
    /// </summary>
    /// <remarks>
    /// Expects an X-Tenant-Id header (Guid) to scope the lookup to the correct tenant.
    ///
    /// Rate limited to 10 requests per 60-second sliding window per client IP.
    /// Returns HTTP 429 if the limit is exceeded.
    ///
    /// Error responses:
    ///   400 — invalid credentials, account locked, or domain rule violated
    ///   422 — input validation failure (missing or malformed field)
    ///   429 — rate limit exceeded
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(request.Email, request.Password);

        LoginResult result = await _sender.Send(command, cancellationToken);

        var response = new LoginResponse(
            result.AccessToken,
            result.UserId,
            result.Name,
            result.Email,
            result.Role);

        return Ok(response);
    }

    /// <summary>
    /// Register a new consumer (end-user) account — no organisation affiliation required.
    /// </summary>
    /// <remarks>
    /// Does NOT require an X-Tenant-Id header.  The created user has TenantId = Guid.Empty,
    /// meaning they can join queues from any organisation.
    ///
    /// Returns 201 Created on success.
    ///
    /// Error responses:
    ///   400 — email already in use
    ///   422 — validation failure
    /// </remarks>
    [HttpPost("register-global")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterGlobal(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterGlobalUserCommand(
            request.Name,
            request.Email,
            request.Phone,
            request.Password);

        await _sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Authenticate a consumer (end-user) account — no organisation affiliation required.
    /// </summary>
    /// <remarks>
    /// Does NOT require an X-Tenant-Id header.  The returned JWT has tid = Guid.Empty;
    /// the frontend supplies X-Tenant-Id per-request for org-specific API calls.
    ///
    /// Rate limited to 10 requests per 60-second sliding window per client IP.
    ///
    /// Error responses:
    ///   400 — invalid credentials or account locked
    ///   422 — validation failure
    ///   429 — rate limit exceeded
    /// </remarks>
    [HttpPost("login-global")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginGlobal(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginGlobalUserCommand(request.Email, request.Password);

        LoginResult result = await _sender.Send(command, cancellationToken);

        var response = new LoginResponse(
            result.AccessToken,
            result.UserId,
            result.Name,
            result.Email,
            result.Role);

        return Ok(response);
    }

    /// <summary>
    /// Create a staff account inside the authenticated org admin's organisation.
    /// </summary>
    [HttpPost("staff")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateStaffUser(
        [FromBody] CreateStaffUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStaffUserCommand(
            request.Name,
            request.Email,
            request.Phone,
            request.Password);

        await _sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Invite a staff user to set up their own password via secure invite link.
    /// </summary>
    [HttpPost("staff/invite")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> InviteStaffUser(
        [FromBody] InviteStaffUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new InviteStaffUserCommand(request.Name, request.Email, request.Phone);
        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Accept a staff invite and set the initial password.
    /// </summary>
    [HttpPost("staff/invite/accept")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptStaffInvite(
        [FromBody] AcceptStaffInviteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AcceptStaffInviteCommand(request.Token, request.Password);
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// List staff accounts for the authenticated organisation.
    /// </summary>
    [HttpGet("staff")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<StaffUserSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListStaffUsers(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListStaffUsersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deactivate a staff account in the authenticated organisation.
    /// </summary>
    [HttpPost("staff/{userId:guid}/deactivate")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateStaffUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateStaffUserCommand(userId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactivate a staff account in the authenticated organisation.
    /// </summary>
    [HttpPost("staff/{userId:guid}/reactivate")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReactivateStaffUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ReactivateStaffUserCommand(userId), cancellationToken);
        return NoContent();
    }
}
