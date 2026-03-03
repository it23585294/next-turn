namespace NextTurn.API.Models.Queues;

/// <summary>
/// Request body for POST /api/queues.
/// OrganisationId is NOT included here — it is read from the authenticated
/// user's JWT <c>tid</c> claim so an org admin can only create queues for
/// their own organisation (prevents cross-org queue creation).
/// </summary>
public sealed record CreateQueueRequest(
    string Name,
    int    MaxCapacity,
    int    AverageServiceTimeSeconds);
