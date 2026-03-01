using MediatR;
using Microsoft.AspNetCore.Mvc;
using NextTurn.API.Models.Auth;
using NextTurn.Application.Auth.Commands.RegisterUser;

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
}
