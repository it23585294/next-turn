using FluentValidation;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentBookingContext;

public sealed class GetAppointmentBookingContextQueryValidator : AbstractValidator<GetAppointmentBookingContextQuery>
{
    public GetAppointmentBookingContextQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.AppointmentProfileId)
            .NotEmpty().WithMessage("Appointment profile ID is required.");
    }
}
