namespace NextTurn.Application.Queue.Commands.CreateQueue;

/// <summary>
/// Returned by <see cref="CreateQueueCommandHandler"/> on a successful queue creation.
///
/// <para>
/// <b>QueueId</b> — the newly generated queue GUID. Stored client-side so the admin
/// can link to the queue list without re-fetching.
/// </para>
/// <para>
/// <b>ShareableLink</b> — the full relative URL that users navigate to in order to join
/// this queue. Format: <c>/queues/{tenantId}/{queueId}</c>.
/// The admin copies and distributes this link (e.g. prints it as a QR code).
/// </para>
/// </summary>
public sealed record CreateQueueResult(
    Guid   QueueId,
    string ShareableLink);
