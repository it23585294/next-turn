using MediatR;

namespace NextTurn.Application.Appointment.Queries.GetMyAppointments;

public sealed record GetMyAppointmentsQuery(Guid UserId) : IRequest<IReadOnlyList<MyAppointmentBooking>>;
