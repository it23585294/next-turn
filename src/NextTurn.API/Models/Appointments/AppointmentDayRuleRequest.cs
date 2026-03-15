namespace NextTurn.API.Models.Appointments;

public sealed record AppointmentDayRuleRequest(
    int DayOfWeek,
    bool IsEnabled,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes);