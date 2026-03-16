namespace NextTurn.Application.Queue.Common;

public sealed record QueueStaffAssignmentDto(
    Guid StaffUserId,
    string Name,
    string Email,
    bool IsActive);
