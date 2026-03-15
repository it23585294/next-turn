using NextTurn.Application.Appointment.Common;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentSchedule;

public sealed record GetAppointmentScheduleResult(
    string ShareableLink,
    IReadOnlyList<AppointmentDayRuleDto> DayRules);