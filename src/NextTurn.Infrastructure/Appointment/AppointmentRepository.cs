using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Appointment.Enums;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Infrastructure.Persistence;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;

namespace NextTurn.Infrastructure.Appointment;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly ApplicationDbContext _context;

    private static readonly AppointmentStatus[] ActiveStatuses =
    {
        AppointmentStatus.Pending,
        AppointmentStatus.Confirmed
    };

    public AppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AppointmentEntity appointment, CancellationToken cancellationToken)
    {
        await _context.Appointments.AddAsync(appointment, cancellationToken);
    }

    public async Task<AppointmentEntity?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
    }

    public Task UpdateAsync(AppointmentEntity appointment, CancellationToken cancellationToken)
    {
        _context.Appointments.Update(appointment);
        return Task.CompletedTask;
    }

    public async Task<bool> HasOverlapAsync(
        Guid organisationId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .AnyAsync(a =>
                    a.OrganisationId == organisationId &&
                    ActiveStatuses.Contains(a.Status) &&
                    a.SlotStart < slotEnd &&
                    slotStart < a.SlotEnd,
                cancellationToken);
    }

    public async Task<bool> HasOverlapExcludingAsync(
        Guid organisationId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        Guid excludedAppointmentId,
        CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .AnyAsync(a =>
                    a.Id != excludedAppointmentId &&
                    a.OrganisationId == organisationId &&
                    ActiveStatuses.Contains(a.Status) &&
                    a.SlotStart < slotEnd &&
                    slotStart < a.SlotEnd,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentEntity>> GetByOrganisationAndDateAsync(
        Guid organisationId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var startOfDay = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Appointments
            .Where(a =>
                a.OrganisationId == organisationId &&
                ActiveStatuses.Contains(a.Status) &&
                a.SlotStart < endOfDay &&
                startOfDay < a.SlotEnd)
            .OrderBy(a => a.SlotStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentScheduleRule>> GetScheduleRulesAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await _context.AppointmentScheduleRules
            .Where(r => r.OrganisationId == organisationId)
            .OrderBy(r => r.DayOfWeek)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertScheduleRulesAsync(
        Guid organisationId,
        IReadOnlyList<AppointmentScheduleRule> rules,
        CancellationToken cancellationToken)
    {
        var existing = await _context.AppointmentScheduleRules
            .Where(r => r.OrganisationId == organisationId)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var matched = existing.FirstOrDefault(e => e.DayOfWeek == rule.DayOfWeek);
            if (matched is null)
            {
                await _context.AppointmentScheduleRules.AddAsync(rule, cancellationToken);
                continue;
            }

            matched.Configure(
                rule.IsEnabled,
                rule.StartTime,
                rule.EndTime,
                rule.SlotDurationMinutes);
        }
    }
}
