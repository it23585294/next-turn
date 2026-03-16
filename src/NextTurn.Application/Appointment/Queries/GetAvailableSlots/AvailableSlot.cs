namespace NextTurn.Application.Appointment.Queries.GetAvailableSlots;

public sealed record AvailableSlot(
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd,
    bool IsBooked);
