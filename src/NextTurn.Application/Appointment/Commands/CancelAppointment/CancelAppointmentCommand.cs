using MediatR;

namespace NextTurn.Application.Appointment.Commands.CancelAppointment;

/// <summary>
/// Command to cancel an appointment owned by the authenticated user.
/// </summary>
public sealed record CancelAppointmentCommand(
    Guid AppointmentId,
    Guid UserId) : IRequest<CancelAppointmentResult>;
