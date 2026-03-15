using MediatR;

namespace NextTurn.Application.Appointment.Commands.CreateAppointmentProfile;

public sealed record CreateAppointmentProfileCommand(
    Guid OrganisationId,
    string Name)
    : IRequest<CreateAppointmentProfileResult>;
