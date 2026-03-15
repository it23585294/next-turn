using MediatR;
using NextTurn.Application.Appointment.Common;
using NextTurn.Domain.Appointment.Repositories;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentSchedule;

public sealed class GetAppointmentScheduleQueryHandler
    : IRequestHandler<GetAppointmentScheduleQuery, GetAppointmentScheduleResult>
{
    private static readonly TimeOnly DefaultStart = new(9, 0);
    private static readonly TimeOnly DefaultEnd = new(17, 0);
    private const int DefaultSlotMinutes = 30;

    private readonly IAppointmentRepository _appointmentRepository;

    public GetAppointmentScheduleQueryHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<GetAppointmentScheduleResult> Handle(
        GetAppointmentScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var configured = await _appointmentRepository.GetScheduleRulesAsync(
            request.OrganisationId,
            request.AppointmentProfileId,
            cancellationToken);

        var dayRules = Enumerable.Range(0, 7)
            .Select(day =>
            {
                var found = configured.FirstOrDefault(r => r.DayOfWeek == day);
                if (found is not null)
                {
                    return new AppointmentDayRuleDto(
                        found.DayOfWeek,
                        found.IsEnabled,
                        found.StartTime,
                        found.EndTime,
                        found.SlotDurationMinutes);
                }

                bool enabled = day is >= 1 and <= 5; // Monday-Friday default
                return new AppointmentDayRuleDto(
                    day,
                    enabled,
                    DefaultStart,
                    DefaultEnd,
                    DefaultSlotMinutes);
            })
            .ToList();

        return new GetAppointmentScheduleResult(
            ShareableLink: $"/appointments/{request.OrganisationId}/{request.AppointmentProfileId}",
            DayRules: dayRules);
    }
}