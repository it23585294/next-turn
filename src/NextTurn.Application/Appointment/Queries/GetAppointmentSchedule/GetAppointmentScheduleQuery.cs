using MediatR;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentSchedule;

public sealed record GetAppointmentScheduleQuery(Guid OrganisationId, Guid AppointmentProfileId)
    : IRequest<GetAppointmentScheduleResult>;