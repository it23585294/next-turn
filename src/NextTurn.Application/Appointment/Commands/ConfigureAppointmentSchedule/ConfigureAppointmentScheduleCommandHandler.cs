using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;

namespace NextTurn.Application.Appointment.Commands.ConfigureAppointmentSchedule;

public sealed class ConfigureAppointmentScheduleCommandHandler
    : IRequestHandler<ConfigureAppointmentScheduleCommand, ConfigureAppointmentScheduleResult>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IApplicationDbContext _context;

    public ConfigureAppointmentScheduleCommandHandler(
        IAppointmentRepository appointmentRepository,
        IApplicationDbContext context)
    {
        _appointmentRepository = appointmentRepository;
        _context = context;
    }

    public async Task<ConfigureAppointmentScheduleResult> Handle(
        ConfigureAppointmentScheduleCommand request,
        CancellationToken cancellationToken)
    {
        var rules = request.DayRules
            .Select(r => AppointmentScheduleRule.Create(
                request.OrganisationId,
                request.AppointmentProfileId,
                r.DayOfWeek,
                r.IsEnabled,
                r.StartTime,
                r.EndTime,
                r.SlotDurationMinutes))
            .ToList();

        await _appointmentRepository.UpsertScheduleRulesAsync(
            request.OrganisationId,
            request.AppointmentProfileId,
            rules,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ConfigureAppointmentScheduleResult(
            ShareableLink: $"/appointments/{request.OrganisationId}/{request.AppointmentProfileId}");
    }
}