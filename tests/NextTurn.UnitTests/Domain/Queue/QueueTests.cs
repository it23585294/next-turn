using FluentAssertions;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Enums;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.UnitTests.Domain.Queue;

/// <summary>
/// Unit tests for the Queue aggregate root.
/// Covers the factory method, property initialisation, and the two behaviour methods.
/// </summary>
public sealed class QueueTests
{
    // ── Shared valid inputs ───────────────────────────────────────────────────

    private static readonly Guid ValidOrgId = Guid.NewGuid();
    private const string ValidName              = "Main Queue";
    private const int    ValidMaxCapacity       = 50;
    private const int    ValidAvgServiceTimeSecs = 300;

    // ── Queue.Create happy path ───────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_SetsAllProperties()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, ValidAvgServiceTimeSecs);

        queue.OrganisationId.Should().Be(ValidOrgId);
        queue.Name.Should().Be(ValidName);
        queue.MaxCapacity.Should().Be(ValidMaxCapacity);
        queue.AverageServiceTimeSeconds.Should().Be(ValidAvgServiceTimeSecs);
    }

    [Fact]
    public void Create_SetsStatusToActive()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, ValidAvgServiceTimeSecs);

        queue.Status.Should().Be(QueueStatus.Active);
    }

    [Fact]
    public void Create_GeneratesNonEmptyId()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, ValidAvgServiceTimeSecs);

        queue.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var q1 = QueueEntity.Create(ValidOrgId, "Queue A", ValidMaxCapacity, ValidAvgServiceTimeSecs);
        var q2 = QueueEntity.Create(ValidOrgId, "Queue B", ValidMaxCapacity, ValidAvgServiceTimeSecs);

        q1.Id.Should().NotBe(q2.Id);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var queue  = QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, ValidAvgServiceTimeSecs);
        var after  = DateTimeOffset.UtcNow;

        queue.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_TrimsLeadingAndTrailingWhitespaceFromName()
    {
        var queue = QueueEntity.Create(ValidOrgId, "  Main Queue  ", ValidMaxCapacity, ValidAvgServiceTimeSecs);

        queue.Name.Should().Be("Main Queue");
    }

    // ── Queue.Create validation failures ─────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsDomainException(string name)
    {
        var act = () => QueueEntity.Create(ValidOrgId, name, ValidMaxCapacity, ValidAvgServiceTimeSecs);

        act.Should().Throw<DomainException>()
           .WithMessage("Queue name is required.");
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var longName = new string('x', 201);

        var act = () => QueueEntity.Create(ValidOrgId, longName, ValidMaxCapacity, ValidAvgServiceTimeSecs);

        act.Should().Throw<DomainException>()
           .WithMessage("Queue name must not exceed 200 characters.");
    }

    [Fact]
    public void Create_WithNameExactly200Chars_Succeeds()
    {
        var name = new string('x', 200);

        var act = () => QueueEntity.Create(ValidOrgId, name, ValidMaxCapacity, ValidAvgServiceTimeSecs);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Create_WithMaxCapacityLessThanOne_ThrowsDomainException(int maxCapacity)
    {
        var act = () => QueueEntity.Create(ValidOrgId, ValidName, maxCapacity, ValidAvgServiceTimeSecs);

        act.Should().Throw<DomainException>()
           .WithMessage("Queue capacity must be at least 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Create_WithAvgServiceTimeLessThanOne_ThrowsDomainException(int avgSecs)
    {
        var act = () => QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, avgSecs);

        act.Should().Throw<DomainException>()
           .WithMessage("Average service time must be at least 1 second.");
    }

    // ── CanAcceptEntry ────────────────────────────────────────────────────────

    [Fact]
    public void CanAcceptEntry_WhenActiveCountBelowCapacity_ReturnsTrue()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, maxCapacity: 10, ValidAvgServiceTimeSecs);

        queue.CanAcceptEntry(activeCount: 9).Should().BeTrue();
    }

    [Fact]
    public void CanAcceptEntry_WhenActiveCountEqualsCapacity_ReturnsFalse()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, maxCapacity: 10, ValidAvgServiceTimeSecs);

        queue.CanAcceptEntry(activeCount: 10).Should().BeFalse();
    }

    [Fact]
    public void CanAcceptEntry_WhenActiveCountExceedsCapacity_ReturnsFalse()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, maxCapacity: 10, ValidAvgServiceTimeSecs);

        queue.CanAcceptEntry(activeCount: 11).Should().BeFalse();
    }

    [Fact]
    public void CanAcceptEntry_WhenQueueIsEmpty_ReturnsTrue()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, maxCapacity: 1, ValidAvgServiceTimeSecs);

        queue.CanAcceptEntry(activeCount: 0).Should().BeTrue();
    }

    // ── CalculateEtaSeconds ───────────────────────────────────────────────────

    [Fact]
    public void CalculateEtaSeconds_AtPositionOne_ReturnsAvgServiceTime()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, averageServiceTimeSeconds: 300);

        queue.CalculateEtaSeconds(position: 1).Should().Be(300);
    }

    [Fact]
    public void CalculateEtaSeconds_AtPositionFive_ReturnsPositionTimesAvgServiceTime()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, averageServiceTimeSeconds: 120);

        queue.CalculateEtaSeconds(position: 5).Should().Be(600);
    }

    [Fact]
    public void CalculateEtaSeconds_IsLinear_DoublePositionDoublesEta()
    {
        var queue = QueueEntity.Create(ValidOrgId, ValidName, ValidMaxCapacity, averageServiceTimeSeconds: 60);

        var etaPosition2 = queue.CalculateEtaSeconds(position: 2);
        var etaPosition4 = queue.CalculateEtaSeconds(position: 4);

        etaPosition4.Should().Be(etaPosition2 * 2);
    }
}
