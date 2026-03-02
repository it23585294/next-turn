using MediatR;

namespace NextTurn.Application.Queue.Queries.GetQueueStatus;

/// <summary>
/// Query to retrieve the authenticated user's current status inside a queue.
///
/// <para>
/// <b>QueueId</b> — parsed from the URL route parameter <c>/api/queues/{queueId}/status</c>.
/// </para>
/// <para>
/// <b>UserId</b> — extracted from the authenticated user's JWT <c>sub</c> claim by the
/// controller and injected here. The handler never reads ClaimsPrincipal directly,
/// keeping it testable without an HttpContext.
/// </para>
/// </summary>
public record GetQueueStatusQuery(
    Guid QueueId,
    Guid UserId) : IRequest<GetQueueStatusResult>;
