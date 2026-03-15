using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Appointment.Commands.RescheduleAppointment;

/// <summary>
/// Handles moving an appointment to another available slot.
/// </summary>
public sealed class RescheduleAppointmentCommandHandler
    : IRequestHandler<RescheduleAppointmentCommand, RescheduleAppointmentResult>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IApplicationDbContext _context;
    private readonly IPublisher _publisher;

    public RescheduleAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IApplicationDbContext context,
        IPublisher publisher)
    {
        _appointmentRepository = appointmentRepository;
        _context = context;
        _publisher = publisher;
    }

    public async Task<RescheduleAppointmentResult> Handle(
        RescheduleAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken);

        if (appointment is null)
            throw new DomainException("Appointment not found.");

        if (appointment.UserId != request.UserId)
            throw new DomainException("You can only reschedule your own appointment.");

        bool hasOverlap = await _appointmentRepository.HasOverlapExcludingAsync(
            appointment.OrganisationId,
            appointment.AppointmentProfileId,
            request.NewSlotStart,
            request.NewSlotEnd,
            appointment.Id,
            cancellationToken);

        if (hasOverlap)
            throw new ConflictDomainException("This time slot is already booked.");

        var oldSlotStart = appointment.SlotStart;
        var oldSlotEnd = appointment.SlotEnd;

        var replacementAppointment = appointment.Reschedule(
            request.NewSlotStart,
            request.NewSlotEnd);

        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);
        await _appointmentRepository.AddAsync(replacementAppointment, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSlotConflict(ex))
        {
            throw new ConflictDomainException("This time slot is already booked.");
        }

        await _publisher.Publish(
            new AppointmentRescheduledNotification(
                PreviousAppointmentId: appointment.Id,
                NewAppointmentId: replacementAppointment.Id,
                UserId: appointment.UserId,
                OrganisationId: appointment.OrganisationId,
                PreviousSlotStart: oldSlotStart,
                PreviousSlotEnd: oldSlotEnd,
                NewSlotStart: replacementAppointment.SlotStart,
                NewSlotEnd: replacementAppointment.SlotEnd),
            cancellationToken);

        return new RescheduleAppointmentResult(
            replacementAppointment.Id,
            replacementAppointment.SlotStart,
            replacementAppointment.SlotEnd);
    }

    private static bool IsSlotConflict(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains(
                   "UX_Appointments_OrganisationId_ProfileId_SlotStart_SlotEnd_Active",
                   StringComparison.OrdinalIgnoreCase)
               || message.Contains(
                   "UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active",
                   StringComparison.OrdinalIgnoreCase);
    }
}
