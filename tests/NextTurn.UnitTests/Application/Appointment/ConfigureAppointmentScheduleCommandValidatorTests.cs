using FluentAssertions;
using NextTurn.Application.Appointment.Commands.ConfigureAppointmentSchedule;
using NextTurn.Application.Appointment.Common;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class ConfigureAppointmentScheduleCommandValidatorTests
{
    private readonly ConfigureAppointmentScheduleCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithSevenDistinctValidDayRules_IsValid()
    {
        var command = new ConfigureAppointmentScheduleCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Enumerable.Range(0, 7)
                .Select(d => new AppointmentDayRuleDto(d, true, new TimeOnly(9, 0), new TimeOnly(17, 0), 30))
                .ToList());

        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenNotSevenRules_HasCountError()
    {
        var command = new ConfigureAppointmentScheduleCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Enumerable.Range(0, 6)
                .Select(d => new AppointmentDayRuleDto(d, true, new TimeOnly(9, 0), new TimeOnly(17, 0), 30))
                .ToList());

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == "Exactly 7 day rules are required.");
    }

    [Fact]
    public async Task ValidateAsync_WhenDuplicateDayRules_HasUniqueDayError()
    {
        var rules = Enumerable.Range(0, 7)
            .Select(d => new AppointmentDayRuleDto(d == 6 ? 5 : d, true, new TimeOnly(9, 0), new TimeOnly(17, 0), 30))
            .ToList();
        var command = new ConfigureAppointmentScheduleCommand(Guid.NewGuid(), Guid.NewGuid(), rules);

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == "Day rules must contain unique DayOfWeek values (0-6).");
    }

    [Fact]
    public async Task ValidateAsync_WhenEnabledAndEndNotAfterStart_HasTimeRangeError()
    {
        var rules = Enumerable.Range(0, 7)
            .Select(d => new AppointmentDayRuleDto(d, true, new TimeOnly(10, 0), new TimeOnly(10, 0), 30))
            .ToList();
        var command = new ConfigureAppointmentScheduleCommand(Guid.NewGuid(), Guid.NewGuid(), rules);

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.ErrorMessage == "End time must be after start time for enabled days.");
    }
}