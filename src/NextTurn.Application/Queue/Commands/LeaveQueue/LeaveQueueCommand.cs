using MediatR;

namespace NextTurn.Application.Queue.Commands.LeaveQueue;

/// <summary>
/// Command representing the intent of a user to leave (cancel their entry in) a specific queue.
///
/// <para>
/// <b>QueueId</b> — parsed from the URL route parameter <c>/api/queues/{queueId}/leave</c>.
/// </para>
/// <para>
/// <b>UserId</b> — extracted from the authenticated user's JWT <c>sub</c> claim by the
/// controller and injected here. The handler never reads ClaimsPrincipal directly,
/// keeping it testable without an HttpContext.
/// </para>
///
/// Input validation (non-empty GUIDs) runs automatically via ValidationBehavior before
/// the handler is invoked.
/// </summary>
public record LeaveQueueCommand(
    Guid QueueId,
    Guid UserId
) : IRequest<Unit>;
