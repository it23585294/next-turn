using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTurn.Application.Queue.Commands.JoinQueue;

namespace NextTurn.API.Controllers;

/// <summary>
/// Handles queue resource endpoints.
///
/// The controller is intentionally thin — it:
///   1. Extracts the authenticated user's ID from the JWT claims
///   2. Maps the HTTP request to a command
///   3. Sends through MediatR
///   4. Maps the result to an HTTP response
///
/// All business logic lives in the Application layer handlers.
/// </summary>
[ApiController]
[Route("api/queues")]
[Authorize]
public sealed class QueuesController : ControllerBase
{
    private readonly ISender _sender;

    public QueuesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Join a queue and receive a numbered ticket.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (Authorization: Bearer {token}) and an X-Tenant-Id header.
    ///
    /// The authenticated user's ID is extracted from the JWT <c>sub</c> claim —
    /// the client never submits a userId in the request body (prevents impersonation).
    ///
    /// Success response:
    /// <code>
    /// {
    ///   "ticketNumber": 42,
    ///   "positionInQueue": 3,
    ///   "estimatedWaitSeconds": 180
    /// }
    /// </code>
    ///
    /// Error responses:
    ///   400 — queue not found
    ///   401 — missing or invalid JWT
    ///   409 — already in this queue, OR queue is full:
    ///          full response includes <c>{ "canBookAppointment": true }</c>
    ///   422 — validation failed (malformed queueId GUID)
    /// </remarks>
    [HttpPost("{queueId:guid}/join")]
    [ProducesResponseType(typeof(JoinQueueResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> JoinQueue(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        // Extract the user's ID from the "sub" claim in the validated JWT.
        // The controller never trusts a userId from the request body — the JWT is
        // already authorised and verified by the bearer middleware above.
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new JoinQueueCommand(queueId, userId);
        var result  = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }
}
