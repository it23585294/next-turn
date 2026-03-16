using FluentValidation;

namespace NextTurn.Application.Appointment.Commands.BookAppointment;

public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.AppointmentProfileId)
            .NotEmpty().WithMessage("Appointment profile ID is required.");

        RuleFor(x => x.SlotStart)
            .NotEmpty().WithMessage("Slot start is required.");

        RuleFor(x => x.SlotEnd)
            .NotEmpty().WithMessage("Slot end is required.")
            .GreaterThan(x => x.SlotStart).WithMessage("Slot end must be after slot start.");
    }
}
