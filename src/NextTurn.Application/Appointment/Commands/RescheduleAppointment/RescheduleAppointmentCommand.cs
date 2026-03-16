using MediatR;

namespace NextTurn.Application.Appointment.Commands.RescheduleAppointment;

/// <summary>
/// Command to move an existing appointment to another available slot.
/// </summary>
public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    Guid UserId,
    DateTimeOffset NewSlotStart,
    DateTimeOffset NewSlotEnd) : IRequest<RescheduleAppointmentResult>;
