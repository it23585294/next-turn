using NextTurn.Domain.Common;

namespace NextTurn.Domain.Queue.Entities;

/// <summary>
/// Assignment of a staff user to a queue they are allowed to operate.
/// </summary>
public sealed class QueueStaffAssignment
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid QueueId { get; private set; }
    public Guid StaffUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private QueueStaffAssignment()
    {
    }

    private QueueStaffAssignment(
        Guid id,
        Guid organisationId,
        Guid queueId,
        Guid staffUserId,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganisationId = organisationId;
        QueueId = queueId;
        StaffUserId = staffUserId;
        CreatedAt = createdAt;
    }

    public static QueueStaffAssignment Create(Guid organisationId, Guid queueId, Guid staffUserId)
    {
        if (organisationId == Guid.Empty)
            throw new DomainException("Organisation ID is required.");

        if (queueId == Guid.Empty)
            throw new DomainException("Queue ID is required.");

        if (staffUserId == Guid.Empty)
            throw new DomainException("Staff user ID is required.");

        return new QueueStaffAssignment(
            Guid.NewGuid(),
            organisationId,
            queueId,
            staffUserId,
            DateTimeOffset.UtcNow);
    }
}
