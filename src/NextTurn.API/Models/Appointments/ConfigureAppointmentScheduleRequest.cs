namespace NextTurn.API.Models.Appointments;

public sealed record ConfigureAppointmentScheduleRequest(
    IReadOnlyList<AppointmentDayRuleRequest> DayRules);