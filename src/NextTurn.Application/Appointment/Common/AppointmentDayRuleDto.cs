namespace NextTurn.Application.Appointment.Common;

public sealed record AppointmentDayRuleDto(
    int DayOfWeek,
    bool IsEnabled,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes);