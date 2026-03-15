using MediatR;

namespace NextTurn.Application.Appointment.Commands.CancelAppointment;

/// <summary>
/// In-process event published after an appointment is cancelled.
/// </summary>
public sealed record AppointmentCancelledNotification(
    Guid AppointmentId,
    Guid UserId,
    Guid OrganisationId,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd,
    bool LateCancellation) : INotification;
