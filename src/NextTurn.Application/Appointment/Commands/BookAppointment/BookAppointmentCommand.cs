using MediatR;

namespace NextTurn.Application.Appointment.Commands.BookAppointment;

/// <summary>
/// Command to reserve a slot in an organisation.
/// </summary>
public sealed record BookAppointmentCommand(
    Guid OrganisationId,
    Guid UserId,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd) : IRequest<BookAppointmentResult>;
