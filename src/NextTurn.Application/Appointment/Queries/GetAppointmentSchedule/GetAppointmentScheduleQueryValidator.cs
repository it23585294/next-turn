using FluentValidation;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentSchedule;

public sealed class GetAppointmentScheduleQueryValidator : AbstractValidator<GetAppointmentScheduleQuery>
{
    public GetAppointmentScheduleQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");
    }
}