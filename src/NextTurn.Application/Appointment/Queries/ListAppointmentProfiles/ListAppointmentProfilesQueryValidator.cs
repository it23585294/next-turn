using FluentValidation;

namespace NextTurn.Application.Appointment.Queries.ListAppointmentProfiles;

public sealed class ListAppointmentProfilesQueryValidator : AbstractValidator<ListAppointmentProfilesQuery>
{
    public ListAppointmentProfilesQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");
    }
}
