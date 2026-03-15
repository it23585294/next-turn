using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Common;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.Application.Appointment.Commands.BookAppointment;

/// <summary>
/// Handles appointment booking flow.
/// </summary>
public sealed class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, BookAppointmentResult>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IApplicationDbContext _context;

    public BookAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IApplicationDbContext context)
    {
        _appointmentRepository = appointmentRepository;
        _context = context;
    }

    public async Task<BookAppointmentResult> Handle(
        BookAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        bool hasOverlap = await _appointmentRepository.HasOverlapAsync(
            request.OrganisationId,
            request.AppointmentProfileId,
            request.SlotStart,
            request.SlotEnd,
            cancellationToken);

        if (hasOverlap)
            throw new ConflictDomainException("This time slot is already booked.");

        var appointment = AppointmentEntity.Create(
            request.OrganisationId,
            request.AppointmentProfileId,
            request.UserId,
            request.SlotStart,
            request.SlotEnd);

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSlotConflict(ex))
        {
            throw new ConflictDomainException("This time slot is already booked.");
        }

        return new BookAppointmentResult(appointment.Id);
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
