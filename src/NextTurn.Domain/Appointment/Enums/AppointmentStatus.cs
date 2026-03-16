namespace NextTurn.Domain.Appointment.Enums;

/// <summary>
/// Lifecycle status for an appointment booking.
/// </summary>
public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Rescheduled
}
