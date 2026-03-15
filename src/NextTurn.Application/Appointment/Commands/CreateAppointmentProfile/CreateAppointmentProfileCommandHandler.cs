using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using AppointmentProfile = NextTurn.Domain.Appointment.Entities.AppointmentProfile;

namespace NextTurn.Application.Appointment.Commands.CreateAppointmentProfile;

public sealed class CreateAppointmentProfileCommandHandler
    : IRequestHandler<CreateAppointmentProfileCommand, CreateAppointmentProfileResult>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IApplicationDbContext _context;

    public CreateAppointmentProfileCommandHandler(
        IAppointmentRepository appointmentRepository,
        IApplicationDbContext context)
    {
        _appointmentRepository = appointmentRepository;
        _context = context;
    }

    public async Task<CreateAppointmentProfileResult> Handle(
        CreateAppointmentProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = AppointmentProfile.Create(request.OrganisationId, request.Name);

        await _appointmentRepository.AddProfileAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateAppointmentProfileResult(
            profile.Id,
            profile.Name,
            profile.IsActive,
            profile.ShareableLink);
    }
}
