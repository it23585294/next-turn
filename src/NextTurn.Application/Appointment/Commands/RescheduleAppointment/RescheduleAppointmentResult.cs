namespace NextTurn.Application.Appointment.Commands.RescheduleAppointment;

/// <summary>
/// Result returned after successful appointment reschedule.
/// </summary>
public sealed record RescheduleAppointmentResult(
    Guid AppointmentId,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd);
