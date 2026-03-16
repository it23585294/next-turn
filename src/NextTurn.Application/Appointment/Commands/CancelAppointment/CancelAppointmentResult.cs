namespace NextTurn.Application.Appointment.Commands.CancelAppointment;

/// <summary>
/// Result returned after a successful cancellation.
/// </summary>
public sealed record CancelAppointmentResult(
    Guid AppointmentId,
    bool LateCancellation);
