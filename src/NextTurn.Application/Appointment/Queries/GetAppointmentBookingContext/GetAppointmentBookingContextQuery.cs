using MediatR;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentBookingContext;

public sealed record GetAppointmentBookingContextQuery(Guid OrganisationId, Guid AppointmentProfileId)
    : IRequest<GetAppointmentBookingContextResult>;
