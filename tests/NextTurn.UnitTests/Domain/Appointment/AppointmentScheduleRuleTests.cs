using FluentAssertions;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Domain.Appointment;

public sealed class AppointmentScheduleRuleTests
{
    [Fact]
    public void Create_WithValidInputs_SetsAllProperties()
    {
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var rule = AppointmentScheduleRule.Create(
            organisationId: orgId,
            appointmentProfileId: profileId,
            dayOfWeek: 1,
            isEnabled: true,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(17, 0),
            slotDurationMinutes: 30);

        rule.OrganisationId.Should().Be(orgId);
        rule.AppointmentProfileId.Should().Be(profileId);
        rule.DayOfWeek.Should().Be(1);
        rule.IsEnabled.Should().BeTrue();
        rule.StartTime.Should().Be(new TimeOnly(9, 0));
        rule.EndTime.Should().Be(new TimeOnly(17, 0));
        rule.SlotDurationMinutes.Should().Be(30);
    }

    [Fact]
    public void Configure_WithValidInputs_UpdatesValues()
    {
        var rule = AppointmentScheduleRule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            3,
            true,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            30);

        rule.Configure(
            isEnabled: false,
            startTime: new TimeOnly(10, 0),
            endTime: new TimeOnly(16, 0),
            slotDurationMinutes: 20);

        rule.IsEnabled.Should().BeFalse();
        rule.StartTime.Should().Be(new TimeOnly(10, 0));
        rule.EndTime.Should().Be(new TimeOnly(16, 0));
        rule.SlotDurationMinutes.Should().Be(20);
    }

    [Fact]
    public void Create_WithInvalidDayOfWeek_Throws()
    {
        var act = () => AppointmentScheduleRule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            7,
            true,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            30);

        act.Should().Throw<DomainException>()
            .WithMessage("DayOfWeek must be between 0 (Sunday) and 6 (Saturday).");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(241)]
    public void Create_WithOutOfRangeSlotDuration_Throws(int duration)
    {
        var act = () => AppointmentScheduleRule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            true,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            duration);

        act.Should().Throw<DomainException>()
            .WithMessage("Slot duration must be between 5 and 240 minutes.");
    }

    [Fact]
    public void Create_WhenEnabledAndEndTimeNotAfterStart_Throws()
    {
        var act = () => AppointmentScheduleRule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            true,
            new TimeOnly(10, 0),
            new TimeOnly(10, 0),
            30);

        act.Should().Throw<DomainException>()
            .WithMessage("End time must be after start time for enabled days.");
    }

    [Fact]
    public void Create_WhenDisabled_AllowsNonIncreasingTimeWindow()
    {
        var act = () => AppointmentScheduleRule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            false,
            new TimeOnly(10, 0),
            new TimeOnly(10, 0),
            30);

        act.Should().NotThrow();
    }
}