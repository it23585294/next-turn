using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTurn.API.Models.Queues;
using NextTurn.Application.Queue.Commands.CallNext;
using NextTurn.Application.Queue.Commands.CreateQueue;
using NextTurn.Application.Queue.Commands.JoinQueue;
using NextTurn.Application.Queue.Commands.LeaveQueue;
using NextTurn.Application.Queue.Commands.MarkNoShow;
using NextTurn.Application.Queue.Commands.MarkServed;
using NextTurn.Application.Queue.Commands.AssignStaffToQueue;
using NextTurn.Application.Queue.Commands.UnassignStaffFromQueue;
using NextTurn.Application.Queue.Commands;
using NextTurn.Application.Queue.Queries.GetQueueDashboard;
using NextTurn.Application.Queue.Queries.GetMyQueues;
using NextTurn.Application.Queue.Queries.GetQueueStatus;
using NextTurn.Application.Queue.Queries.ListOrgQueues;
using NextTurn.Application.Queue.Queries.ListQueueStaffAssignments;
using NextTurn.Application.Queue.Queries.ListStaffQueues;
using NextTurn.Domain.Queue.Repositories;

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
    private readonly IQueueRepository _queueRepository;

    public QueuesController(ISender sender, IQueueRepository queueRepository)
    {
        _sender = sender;
        _queueRepository = queueRepository;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out userId);
    }

    private bool TryGetOrganisationId(out Guid organisationId)
    {
        var tenantIdClaim = User.FindFirstValue("tid");
        return Guid.TryParse(tenantIdClaim, out organisationId);
    }

    private string? GetRole()
    {
        return User.FindFirstValue(ClaimTypes.Role)
            ?? User.FindFirstValue("role");
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
        // Extract the user's ID from the validated JWT claim.
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var command = new JoinQueueCommand(queueId, userId);
        var result  = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Leave (cancel entry in) a queue.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (Authorization: Bearer {token}) and an X-Tenant-Id header.
    ///
    /// The authenticated user's ID is extracted from the JWT <c>sub</c> claim.
    /// The user may only cancel their own entry.
    ///
    /// Success response: 204 No Content
    ///
    /// Error responses:
    ///   400 — queue not found, or user is not in this queue
    ///   401 — missing or invalid JWT
    ///   422 — validation failed (malformed queueId GUID)
    /// </remarks>
    [HttpPost("{queueId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LeaveQueue(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var command = new LeaveQueueCommand(queueId, userId);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Create a new queue for the authenticated org admin's organisation.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT with role OrgAdmin or SystemAdmin.
    /// OrganisationId is read from the JWT <c>tid</c> claim — admins can only
    /// create queues for their own organisation.
    ///
    /// Success response (201 Created):
    /// <code>
    /// {
    ///   "queueId": "3fa85f64-...",
    ///   "shareableLink": "/queues/{tenantId}/{queueId}"
    /// }
    /// </code>
    ///
    /// Error responses:
    ///   400 — organisation not found
    ///   401 — missing or invalid JWT
    ///   403 — JWT role is not OrgAdmin or SystemAdmin
    ///   422 — validation failed (name empty, capacity &lt; 1, avgTime &lt; 1)
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(CreateQueueResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateQueue(
        [FromBody] CreateQueueRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new CreateQueueCommand(
            OrganisationId:            organisationId,
            Name:                      request.Name,
            MaxCapacity:               request.MaxCapacity,
            AverageServiceTimeSeconds: request.AverageServiceTimeSeconds);

        var result = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetQueueStatus), new { queueId = result.QueueId }, result);
    }

    /// <summary>
    /// List all queues for the authenticated org admin's organisation.
    /// Used by the admin dashboard on page load.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<OrgQueueSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListQueues(CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var query  = new ListOrgQueuesQuery(organisationId);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// List all active queues for the authenticated user's organisation.
    /// Accessible to any authenticated role so regular users can browse
    /// available queues on their dashboard without needing OrgAdmin rights.
    /// </summary>
    [HttpGet("browse")]
    [ProducesResponseType(typeof(IReadOnlyList<OrgQueueSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BrowseQueues(CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var query  = new ListOrgQueuesQuery(organisationId);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns every queue the authenticated user currently has an active ticket in.
    /// Active means the entry status is Waiting or Serving.
    /// </summary>
    [HttpGet("my-entries")]
    [ProducesResponseType(typeof(IReadOnlyList<MyQueueEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyQueues(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var query  = new GetMyQueuesQuery(userId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get the authenticated user's current position and ETA in a queue.
    /// Called by the frontend polling loop every 30 seconds after joining.
    /// </summary>
    /// <remarks>
    /// Requires a valid JWT (any role).
    ///
    /// Success response:
    /// <code>
    /// {
    ///   "ticketNumber": 42,
    ///   "positionInQueue": 2,
    ///   "estimatedWaitSeconds": 120,
    ///   "queueStatus": "Active"
    /// }
    /// </code>
    ///
    /// Error responses:
    ///   400 — queue not found, or user has no active entry in this queue
    ///   401 — missing or invalid JWT
    ///   422 — malformed queueId GUID
    /// </remarks>
    [HttpGet("{queueId:guid}/status")]
    [ProducesResponseType(typeof(GetQueueStatusResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetQueueStatus(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var query  = new GetQueueStatusQuery(queueId, userId);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// List queues available to the authenticated staff user.
    /// Staff users receive only queues assigned to them.
    /// Org admins and system admins receive all org queues.
    /// </summary>
    [HttpGet("staff-assigned")]
    [Authorize(Roles = "Staff,OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<OrgQueueSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListStaffQueues(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var role = GetRole();
        if (string.IsNullOrWhiteSpace(role))
            return Unauthorized();

        var query = new ListStaffQueuesQuery(userId, role, organisationId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists staff accounts currently assigned to a queue.
    /// </summary>
    [HttpGet("{queueId:guid}/staff-assignments")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListQueueStaffAssignments(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        var query = new ListQueueStaffAssignmentsQuery(queueId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Assigns a staff account to a queue.
    /// </summary>
    [HttpPost("{queueId:guid}/staff-assignments/{staffUserId:guid}")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignStaffToQueue(
        Guid queueId,
        Guid staffUserId,
        CancellationToken cancellationToken)
    {
        var command = new AssignStaffToQueueCommand(queueId, staffUserId);
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Removes a staff assignment from a queue.
    /// </summary>
    [HttpDelete("{queueId:guid}/staff-assignments/{staffUserId:guid}")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnassignStaffFromQueue(
        Guid queueId,
        Guid staffUserId,
        CancellationToken cancellationToken)
    {
        var command = new UnassignStaffFromQueueCommand(queueId, staffUserId);
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Returns staff dashboard data for a queue: current serving ticket and waiting list.
    /// </summary>
    [HttpGet("{queueId:guid}/dashboard")]
    [Authorize(Policy = "IsStaff")]
    [ProducesResponseType(typeof(GetQueueDashboardResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetQueueDashboard(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var role = GetRole();
        if (role == "Staff")
        {
            var isAssigned = await _queueRepository.IsStaffAssignedToQueueAsync(queueId, userId, cancellationToken);
            if (!isAssigned)
                return Forbid();
        }

        var query = new GetQueueDashboardQuery(queueId);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Calls the next waiting ticket into service.
    /// </summary>
    [HttpPost("{queueId:guid}/call-next")]
    [Authorize(Policy = "IsStaff")]
    [ProducesResponseType(typeof(QueueEntryActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CallNext(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var role = GetRole();
        if (role == "Staff")
        {
            var isAssigned = await _queueRepository.IsStaffAssignedToQueueAsync(queueId, userId, cancellationToken);
            if (!isAssigned)
                return Forbid();
        }

        var command = new CallNextCommand(queueId);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Marks the currently serving ticket as served.
    /// </summary>
    [HttpPost("{queueId:guid}/served")]
    [Authorize(Policy = "IsStaff")]
    [ProducesResponseType(typeof(QueueEntryActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkServed(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var role = GetRole();
        if (role == "Staff")
        {
            var isAssigned = await _queueRepository.IsStaffAssignedToQueueAsync(queueId, userId, cancellationToken);
            if (!isAssigned)
                return Forbid();
        }

        var command = new MarkServedCommand(queueId);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Marks the currently serving ticket as no-show.
    /// </summary>
    [HttpPost("{queueId:guid}/no-show")]
    [Authorize(Policy = "IsStaff")]
    [ProducesResponseType(typeof(QueueEntryActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkNoShow(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var role = GetRole();
        if (role == "Staff")
        {
            var isAssigned = await _queueRepository.IsStaffAssignedToQueueAsync(queueId, userId, cancellationToken);
            if (!isAssigned)
                return Forbid();
        }

        var command = new MarkNoShowCommand(queueId);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
