using MediatR;
using Microsoft.Extensions.Logging;

namespace NextTurn.Application.Appointment.Commands.RescheduleAppointment;

/// <summary>
/// Stub notification handler for appointment reschedules.
/// Real outbound delivery (email/SMS) is deferred to the Notifications epic.
/// </summary>
public sealed class AppointmentRescheduledNotificationHandler
    : INotificationHandler<AppointmentRescheduledNotification>
{
    private readonly ILogger<AppointmentRescheduledNotificationHandler> _logger;

    public AppointmentRescheduledNotificationHandler(
        ILogger<AppointmentRescheduledNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        AppointmentRescheduledNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Appointment rescheduled. OldId: {OldId}, NewId: {NewId}, UserId: {UserId}, OrgId: {OrgId}, OldSlot: {OldStart}->{OldEnd}, NewSlot: {NewStart}->{NewEnd}.",
            notification.PreviousAppointmentId,
            notification.NewAppointmentId,
            notification.UserId,
            notification.OrganisationId,
            notification.PreviousSlotStart,
            notification.PreviousSlotEnd,
            notification.NewSlotStart,
            notification.NewSlotEnd);

        return Task.CompletedTask;
    }
}
