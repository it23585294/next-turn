using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Appointment.Commands.CancelAppointment;

/// <summary>
/// Handles appointment cancellation.
/// </summary>
public sealed class CancelAppointmentCommandHandler
    : IRequestHandler<CancelAppointmentCommand, CancelAppointmentResult>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IApplicationDbContext _context;
    private readonly IPublisher _publisher;

    public CancelAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IApplicationDbContext context,
        IPublisher publisher)
    {
        _appointmentRepository = appointmentRepository;
        _context = context;
        _publisher = publisher;
    }

    public async Task<CancelAppointmentResult> Handle(
        CancelAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken);

        if (appointment is null)
            throw new DomainException("Appointment not found.");

        if (appointment.UserId != request.UserId)
            throw new DomainException("You can only cancel your own appointment.");

        appointment.Cancel();

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new AppointmentCancelledNotification(
                AppointmentId: appointment.Id,
                UserId: appointment.UserId,
                OrganisationId: appointment.OrganisationId,
                SlotStart: appointment.SlotStart,
                SlotEnd: appointment.SlotEnd,
                LateCancellation: appointment.LateCancellation),
            cancellationToken);

        return new CancelAppointmentResult(
            appointment.Id,
            appointment.LateCancellation);
    }
}
