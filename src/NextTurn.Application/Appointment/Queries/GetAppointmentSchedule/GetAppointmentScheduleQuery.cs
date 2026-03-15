using MediatR;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentSchedule;

public sealed record GetAppointmentScheduleQuery(Guid OrganisationId)
    : IRequest<GetAppointmentScheduleResult>;