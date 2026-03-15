using MediatR;
using NextTurn.Domain.Appointment.Repositories;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;

namespace NextTurn.Application.Appointment.Queries.GetAvailableSlots;

public sealed class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<AvailableSlot>>
{
    private static readonly TimeOnly DefaultDayStart = new(9, 0);
    private static readonly TimeOnly DefaultDayEnd = new(17, 0);
    private const int DefaultSlotDurationMinutes = 30;

    private readonly IAppointmentRepository _appointmentRepository;

    public GetAvailableSlotsQueryHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<IReadOnlyList<AvailableSlot>> Handle(
        GetAvailableSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await _appointmentRepository.GetScheduleRulesAsync(
            request.OrganisationId,
            cancellationToken)
            ?? Array.Empty<AppointmentScheduleRule>();

        var booked = await _appointmentRepository.GetByOrganisationAndDateAsync(
            request.OrganisationId,
            request.Date,
            cancellationToken);

        var configuredRule = ResolveRuleForDate(rules, request.OrganisationId, request.Date.DayOfWeek);
        if (!configuredRule.IsEnabled)
            return Array.Empty<AvailableSlot>();

        var slots = new List<AvailableSlot>();

        var slotDuration = TimeSpan.FromMinutes(configuredRule.SlotDurationMinutes);
        var startOfDay = new DateTimeOffset(request.Date.ToDateTime(configuredRule.StartTime), TimeSpan.Zero);
        var endOfDay = new DateTimeOffset(request.Date.ToDateTime(configuredRule.EndTime), TimeSpan.Zero);

        for (var cursor = startOfDay; cursor.Add(slotDuration) <= endOfDay; cursor = cursor.Add(slotDuration))
        {
            var slotStart = cursor;
            var slotEnd = cursor.Add(slotDuration);

            bool isBooked = booked.Any(a => a.Overlaps(slotStart, slotEnd));
            slots.Add(new AvailableSlot(slotStart, slotEnd, isBooked));
        }

        return slots;
    }

    private static AppointmentScheduleRule ResolveRuleForDate(
        IReadOnlyList<AppointmentScheduleRule> rules,
        Guid organisationId,
        DayOfWeek dayOfWeek)
    {
        int day = (int)dayOfWeek;
        var configured = rules.FirstOrDefault(r => r.DayOfWeek == day);
        if (configured is not null)
            return configured;

        // Fallback defaults for organisations that have not configured schedule yet.
        bool enabled = day is >= 1 and <= 5; // Monday-Friday

        return AppointmentScheduleRule.Create(
            organisationId,
            day,
            enabled,
            DefaultDayStart,
            DefaultDayEnd,
            DefaultSlotDurationMinutes);
    }
}
