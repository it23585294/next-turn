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
        var start = DateTimeOffset.UtcNow.AddHours(30);
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            start,
            start.AddMinutes(30));

        appointment.Cancel();

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.LateCancellation.Should().BeFalse();
    }

    [Fact]
    public void Cancel_Within24Hours_SetsLateCancellationTrue()
    {
        var start = DateTimeOffset.UtcNow.AddHours(12);
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            start,
            start.AddMinutes(30));

        appointment.Cancel();

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.LateCancellation.Should().BeTrue();
    }

    [Fact]
    public void Cancel_WhenPastAppointment_ThrowsDomainException()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1));

        var act = () => appointment.Cancel();

        act.Should().Throw<DomainException>().WithMessage("Past appointments cannot be cancelled.");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsDomainException()
    {
        var start = DateTimeOffset.UtcNow.AddHours(2);
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            start,
            start.AddMinutes(30));

        appointment.Cancel();

        var act = () => appointment.Cancel();

        act.Should().Throw<DomainException>().WithMessage("Appointment is already cancelled.");
    }

    [Fact]
    public void Reschedule_WithFutureAppointment_MarksOriginalRescheduledAndReturnsReplacement()
    {
        var originalStart = DateTimeOffset.UtcNow.AddHours(2);
        var originalEnd = originalStart.AddMinutes(30);

        var appointment = AppointmentEntity.Create(OrgId, UserId, originalStart, originalEnd);

        var nextStart = DateTimeOffset.UtcNow.AddHours(5);
        var nextEnd = nextStart.AddMinutes(45);

        var replacement = appointment.Reschedule(nextStart, nextEnd);

        appointment.Status.Should().Be(AppointmentStatus.Rescheduled);

        replacement.Id.Should().NotBe(appointment.Id);
        replacement.OrganisationId.Should().Be(appointment.OrganisationId);
        replacement.UserId.Should().Be(appointment.UserId);
        replacement.SlotStart.Should().Be(nextStart);
        replacement.SlotEnd.Should().Be(nextEnd);
        replacement.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    [Fact]
    public void Reschedule_WithInvalidRange_Throws()
    {
        var currentStart = DateTimeOffset.UtcNow.AddHours(1);
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            currentStart,
            currentStart.AddMinutes(30));

        var sameStartAndEnd = DateTimeOffset.UtcNow.AddHours(4);

        var act = () => appointment.Reschedule(
            sameStartAndEnd,
            sameStartAndEnd);

        act.Should().Throw<DomainException>().WithMessage("Slot end must be after slot start.");
    }

    [Fact]
    public void Reschedule_WhenOriginalAppointmentIsPast_ThrowsDomainException()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1));

        var act = () => appointment.Reschedule(
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(1).AddMinutes(30));

        act.Should().Throw<DomainException>().WithMessage("Past appointments cannot be rescheduled.");
    }

    [Fact]
    public void Reschedule_WhenNewSlotIsPast_ThrowsDomainException()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(1).AddMinutes(30));

        var act = () => appointment.Reschedule(
            DateTimeOffset.UtcNow.AddMinutes(-10),
            DateTimeOffset.UtcNow.AddMinutes(20));

        act.Should().Throw<DomainException>().WithMessage("New slot must be in the future.");
    }

    [Fact]
    public void Reschedule_WhenAppointmentNotConfirmed_ThrowsDomainException()
    {
        var appointment = AppointmentEntity.Create(
            OrgId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(1).AddMinutes(30));

        appointment.Cancel();

        var act = () => appointment.Reschedule(
            DateTimeOffset.UtcNow.AddHours(3),
            DateTimeOffset.UtcNow.AddHours(3).AddMinutes(30));

        act.Should().Throw<DomainException>().WithMessage("Only confirmed appointments can be rescheduled.");
    }
}
