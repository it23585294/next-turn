using FluentValidation;

namespace NextTurn.Application.Appointment.Commands.ConfigureAppointmentSchedule;

public sealed class ConfigureAppointmentScheduleCommandValidator : AbstractValidator<ConfigureAppointmentScheduleCommand>
{
    public ConfigureAppointmentScheduleCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty().WithMessage("Organisation ID is required.");

        RuleFor(x => x.DayRules)
            .NotNull().WithMessage("Day rules are required.")
            .Must(rules => rules is { Count: 7 })
            .WithMessage("Exactly 7 day rules are required.");

        RuleFor(x => x.DayRules)
            .Must(rules => rules.Select(r => r.DayOfWeek).Distinct().Count() == 7)
            .WithMessage("Day rules must contain unique DayOfWeek values (0-6).");

        RuleForEach(x => x.DayRules).ChildRules(day =>
        {
            day.RuleFor(r => r.DayOfWeek)
                .InclusiveBetween(0, 6)
                .WithMessage("DayOfWeek must be between 0 and 6.");

            day.RuleFor(r => r.SlotDurationMinutes)
                .InclusiveBetween(5, 240)
                .WithMessage("Slot duration must be between 5 and 240 minutes.");

            day.RuleFor(r => r)
                .Must(r => !r.IsEnabled || r.EndTime > r.StartTime)
                .WithMessage("End time must be after start time for enabled days.");
        });
    }
}