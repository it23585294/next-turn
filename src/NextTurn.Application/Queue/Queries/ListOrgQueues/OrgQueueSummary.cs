namespace NextTurn.Application.Queue.Queries.ListOrgQueues;

/// <summary>
/// Summary of a single queue returned in the org admin dashboard list.
/// Includes a pre-built shareable link so the frontend can show a copy button
/// without any additional calculation.
/// </summary>
public sealed record OrgQueueSummary(
    Guid   QueueId,
    string Name,
    int    MaxCapacity,
    int    AverageServiceTimeSeconds,
    string Status,
    string ShareableLink);
