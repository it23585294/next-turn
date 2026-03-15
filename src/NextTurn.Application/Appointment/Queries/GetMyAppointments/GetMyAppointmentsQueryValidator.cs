using FluentValidation;

namespace NextTurn.Application.Appointment.Queries.GetMyAppointments;

public sealed class GetMyAppointmentsQueryValidator : AbstractValidator<GetMyAppointmentsQuery>
{
    public GetMyAppointmentsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
