using MediatR;

namespace NextTurn.Application.Appointment.Commands.RescheduleAppointment;

/// <summary>
/// In-process event published after an appointment is successfully rescheduled.
/// </summary>
public sealed record AppointmentRescheduledNotification(
    Guid PreviousAppointmentId,
    Guid NewAppointmentId,
    Guid UserId,
    Guid OrganisationId,
    DateTimeOffset PreviousSlotStart,
    DateTimeOffset PreviousSlotEnd,
    DateTimeOffset NewSlotStart,
    DateTimeOffset NewSlotEnd) : INotification;
