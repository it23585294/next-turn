using FluentValidation;

namespace NextTurn.Application.Appointment.Queries.GetAvailableSlots;

public sealed class GetAvailableSlotsQueryValidator : AbstractValidator<GetAvailableSlotsQuery>
{
    public GetAvailableSlotsQueryValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");
    }
}
