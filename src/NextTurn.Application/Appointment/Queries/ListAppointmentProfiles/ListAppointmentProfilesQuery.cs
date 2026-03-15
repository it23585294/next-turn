using MediatR;
using NextTurn.Application.Appointment.Common;

namespace NextTurn.Application.Appointment.Queries.ListAppointmentProfiles;

public sealed record ListAppointmentProfilesQuery(Guid OrganisationId)
    : IRequest<IReadOnlyList<AppointmentProfileDto>>;
