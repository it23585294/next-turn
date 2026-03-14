namespace NextTurn.API.Models.Appointments;

public sealed record RescheduleAppointmentRequest(
    DateTimeOffset NewSlotStart,
    DateTimeOffset NewSlotEnd);
