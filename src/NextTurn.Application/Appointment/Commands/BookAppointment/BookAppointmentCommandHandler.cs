using MediatR;
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
            request.SlotStart,
            request.SlotEnd,
            cancellationToken);

        if (hasOverlap)
            throw new ConflictDomainException("This time slot is already booked.");

        var appointment = AppointmentEntity.Create(
            request.OrganisationId,
            request.UserId,
            request.SlotStart,
            request.SlotEnd);

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new BookAppointmentResult(appointment.Id);
    }
}
