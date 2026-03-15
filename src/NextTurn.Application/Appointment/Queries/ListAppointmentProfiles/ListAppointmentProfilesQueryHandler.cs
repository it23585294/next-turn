using MediatR;
using NextTurn.Application.Appointment.Common;
using NextTurn.Domain.Appointment.Repositories;

namespace NextTurn.Application.Appointment.Queries.ListAppointmentProfiles;

public sealed class ListAppointmentProfilesQueryHandler
    : IRequestHandler<ListAppointmentProfilesQuery, IReadOnlyList<AppointmentProfileDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;

    public ListAppointmentProfilesQueryHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<IReadOnlyList<AppointmentProfileDto>> Handle(
        ListAppointmentProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var profiles = await _appointmentRepository.GetProfilesByOrganisationAsync(
            request.OrganisationId,
            cancellationToken);

        return profiles
            .Select(p => new AppointmentProfileDto(
                p.Id,
                p.Name,
                p.IsActive,
                p.ShareableLink))
            .ToList();
    }
}
