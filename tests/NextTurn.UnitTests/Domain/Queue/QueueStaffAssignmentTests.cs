using FluentAssertions;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Entities;

namespace NextTurn.UnitTests.Domain.Queue;

public sealed class QueueStaffAssignmentTests
{
    [Fact]
    public void Create_WithValidIds_SetsProperties()
    {
        var organisationId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        var assignment = QueueStaffAssignment.Create(organisationId, queueId, staffUserId);

        var after = DateTimeOffset.UtcNow;
        assignment.Id.Should().NotBeEmpty();
        assignment.OrganisationId.Should().Be(organisationId);
        assignment.QueueId.Should().Be(queueId);
        assignment.StaffUserId.Should().Be(staffUserId);
        assignment.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithEmptyOrganisationId_Throws()
    {
        var act = () => QueueStaffAssignment.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("Organisation ID is required.");
    }

    [Fact]
    public void Create_WithEmptyQueueId_Throws()
    {
        var act = () => QueueStaffAssignment.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("Queue ID is required.");
    }

    [Fact]
    public void Create_WithEmptyStaffUserId_Throws()
    {
        var act = () => QueueStaffAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("Staff user ID is required.");
    }
}