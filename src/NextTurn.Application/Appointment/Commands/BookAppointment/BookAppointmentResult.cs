namespace NextTurn.Application.Appointment.Commands.BookAppointment;

/// <summary>
/// Returned on successful appointment booking.
/// </summary>
public sealed record BookAppointmentResult(Guid AppointmentId);
