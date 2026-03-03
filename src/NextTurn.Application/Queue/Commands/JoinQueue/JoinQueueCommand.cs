using MediatR;

namespace NextTurn.Application.Queue.Commands.JoinQueue;

/// <summary>
/// Command representing the intent of a user to join a specific queue.
///
/// <para>
/// <b>QueueId</b> — parsed from the URL route parameter <c>/api/queues/{queueId}/join</c>.
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
public record JoinQueueCommand(
    Guid QueueId,
    Guid UserId
) : IRequest<JoinQueueResult>;
