using MediatR;
using Microsoft.Extensions.Logging;

namespace NextTurn.Application.Appointment.Commands.CancelAppointment;

/// <summary>
/// Stub notification handler for appointment cancellation events.
/// </summary>
public sealed class AppointmentCancelledNotificationHandler
    : INotificationHandler<AppointmentCancelledNotification>
{
    private readonly ILogger<AppointmentCancelledNotificationHandler> _logger;

    public AppointmentCancelledNotificationHandler(
        ILogger<AppointmentCancelledNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        AppointmentCancelledNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Appointment cancelled. AppointmentId: {AppointmentId}, UserId: {UserId}, OrgId: {OrgId}, Slot: {SlotStart}->{SlotEnd}, LateCancellation: {LateCancellation}.",
            notification.AppointmentId,
            notification.UserId,
            notification.OrganisationId,
            notification.SlotStart,
            notification.SlotEnd,
            notification.LateCancellation);

        return Task.CompletedTask;
    }
}
