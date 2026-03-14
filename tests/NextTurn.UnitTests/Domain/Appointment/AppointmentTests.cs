using FluentAssertions;
using NextTurn.Domain.Appointment.Enums;
using NextTurn.Domain.Common;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.UnitTests.Domain.Appointment;

public sealed class AppointmentTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInput_SetsProperties()
    {
        var slotStart = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);
        var slotEnd = slotStart.AddMinutes(30);

        var appointment = AppointmentEntity.Create(OrgId, UserId, slotStart, slotEnd);

        appointment.Id.Should().NotBeEmpty();
        appointment.OrganisationId.Should().Be(OrgId);
        appointment.UserId.Should().Be(UserId);
        appointment.SlotStart.Should().Be(slotStart);
        appointment.SlotEnd.Should().Be(slotEnd);
        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public void Create_WithEmptyOrganisationId_Throws()
    {
        var slotStart = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);
        var slotEnd = slotStart.AddMinutes(30);

        var act = () => AppointmentEntity.Create(Guid.Empty, UserId, slotStart, slotEnd);

        act.Should().Throw<DomainException>().WithMessage("Organisation ID is required.");
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var slotStart = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);
        var slotEnd = slotStart.AddMinutes(30);

        var act = () => AppointmentEntity.Create(OrgId, Guid.Empty, slotStart, slotEnd);

        act.Should().Throw<DomainException>().WithMessage("User ID is required.");
    }

    [Fact]
    public void Create_WithEndBeforeStart_Throws()
    {
        var slotStart = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);
        var slotEnd = slotStart.AddMinutes(-30);

        var act = () => AppointmentEntity.Create(OrgId, UserId, slotStart, slotEnd);

        act.Should().Throw<DomainException>().WithMessage("Slot end must be after slot start.");
    }

    [Fact]
    public void Overlaps_WithIntersectingRange_ReturnsTrue()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero));

        var overlaps = appointment.Overlaps(
            new DateTimeOffset(2026, 3, 20, 10, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 10, 45, 0, TimeSpan.Zero));

        overlaps.Should().BeTrue();
    }

    [Fact]
    public void Overlaps_WithAdjacentRange_ReturnsFalse()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero));

        var overlaps = appointment.Overlaps(
            new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 11, 0, 0, TimeSpan.Zero));

        overlaps.Should().BeFalse();
    }

    [Fact]
    public void Cancel_SetsStatusCancelled()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero));

        appointment.Cancel();

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public void Reschedule_WithValidRange_UpdatesSlotAndStatus()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero));

        var nextStart = new DateTimeOffset(2026, 3, 21, 11, 0, 0, TimeSpan.Zero);
        var nextEnd = nextStart.AddMinutes(45);

        appointment.Reschedule(nextStart, nextEnd);

        appointment.SlotStart.Should().Be(nextStart);
        appointment.SlotEnd.Should().Be(nextEnd);
        appointment.Status.Should().Be(AppointmentStatus.Rescheduled);
    }

    [Fact]
    public void Reschedule_WithInvalidRange_Throws()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 10, 30, 0, TimeSpan.Zero));

        var act = () => appointment.Reschedule(
            new DateTimeOffset(2026, 3, 21, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 21, 11, 0, 0, TimeSpan.Zero));

        act.Should().Throw<DomainException>().WithMessage("Slot end must be after slot start.");
    }
}
