namespace NextTurn.Application.Queue.Commands;

/// <summary>
/// Minimal response for staff ticket transition actions.
/// </summary>
public sealed record QueueEntryActionResult(
    Guid EntryId,
    int TicketNumber,
    string Status);
