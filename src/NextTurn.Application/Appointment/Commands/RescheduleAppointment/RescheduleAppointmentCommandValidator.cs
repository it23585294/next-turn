using FluentValidation;

namespace NextTurn.Application.Appointment.Commands.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("Appointment ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.NewSlotStart)
            .NotEmpty().WithMessage("New slot start is required.");

        RuleFor(x => x.NewSlotEnd)
            .NotEmpty().WithMessage("New slot end is required.")
            .GreaterThan(x => x.NewSlotStart)
            .WithMessage("New slot end must be after new slot start.");
    }
}
