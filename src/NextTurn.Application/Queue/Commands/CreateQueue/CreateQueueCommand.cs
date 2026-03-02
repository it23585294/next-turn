using MediatR;

namespace NextTurn.Application.Queue.Commands.CreateQueue;

/// <summary>
/// Command for an org admin to create a new queue under their organisation.
///
/// <para>
/// <b>OrganisationId</b> — taken from the authenticated user's JWT <c>tid</c> claim.
/// In NextTurn, the OrgAdmin's TenantId == their OrganisationId, so no separate
/// org-ID input is needed from the request body.
/// </para>
/// <para>
/// <b>Name</b> — human-readable queue label displayed to users joining the queue.
/// </para>
/// <para>
/// <b>MaxCapacity</b> — maximum simultaneous active entries (Waiting + Serving).
/// Once reached, new join attempts receive a 409 with canBookAppointment: true.
/// </para>
/// <para>
/// <b>AverageServiceTimeSeconds</b> — seconds per customer; drives ETA calculations
/// shown to users on the QueuePage.
/// </para>
/// </summary>
public record CreateQueueCommand(
    Guid   OrganisationId,
    string Name,
    int    MaxCapacity,
    int    AverageServiceTimeSeconds) : IRequest<CreateQueueResult>;
