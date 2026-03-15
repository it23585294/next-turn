using FluentValidation;

namespace NextTurn.Application.Appointment.Commands.CreateAppointmentProfile;

public sealed class CreateAppointmentProfileCommandValidator : AbstractValidator<CreateAppointmentProfileCommand>
{
    public CreateAppointmentProfileCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Appointment profile name is required.")
            .MaximumLength(120).WithMessage("Appointment profile name cannot exceed 120 characters.");
    }
}
