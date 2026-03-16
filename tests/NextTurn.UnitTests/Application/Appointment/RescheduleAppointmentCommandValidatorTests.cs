using FluentAssertions;
using NextTurn.Application.Appointment.Commands.RescheduleAppointment;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class RescheduleAppointmentCommandValidatorTests
{
    private readonly RescheduleAppointmentCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_IsValid()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var end = start.AddMinutes(30);

        var result = await _validator.ValidateAsync(
            new RescheduleAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), start, end));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenEndNotAfterStart_HasError()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);

        var result = await _validator.ValidateAsync(
            new RescheduleAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), start, start));

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RescheduleAppointmentCommand.NewSlotEnd) &&
            e.ErrorMessage == "New slot end must be after new slot start.");
    }
}