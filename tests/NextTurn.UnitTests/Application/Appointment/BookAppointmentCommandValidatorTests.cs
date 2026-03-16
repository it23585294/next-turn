using FluentAssertions;
using NextTurn.Application.Appointment.Commands.BookAppointment;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class BookAppointmentCommandValidatorTests
{
    private readonly BookAppointmentCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_IsValid()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var end = start.AddMinutes(30);

        var result = await _validator.ValidateAsync(
            new BookAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, end));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenEndNotAfterStart_HasError()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);

        var result = await _validator.ValidateAsync(
            new BookAppointmentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), start, start));

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(BookAppointmentCommand.SlotEnd) &&
            e.ErrorMessage == "Slot end must be after slot start.");
    }
}