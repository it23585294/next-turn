using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Appointment.Enums;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Infrastructure.Persistence;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentProfile = NextTurn.Domain.Appointment.Entities.AppointmentProfile;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;

namespace NextTurn.Infrastructure.Appointment;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly ApplicationDbContext _context;

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
        Guid appointmentProfileId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .AnyAsync(a =>
                    a.OrganisationId == organisationId &&
                    a.AppointmentProfileId == appointmentProfileId &&
                    (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                    a.SlotStart < slotEnd &&
                    slotStart < a.SlotEnd,
                cancellationToken);
    }

    public async Task<bool> HasOverlapExcludingAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        Guid excludedAppointmentId,
        CancellationToken cancellationToken)
    {
        return await _context.Appointments
            .AnyAsync(a =>
                    a.Id != excludedAppointmentId &&
                    a.OrganisationId == organisationId &&
                    a.AppointmentProfileId == appointmentProfileId &&
                    (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                    a.SlotStart < slotEnd &&
                    slotStart < a.SlotEnd,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentEntity>> GetByOrganisationAndDateAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var startOfDay = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Appointments
            .Where(a =>
                a.OrganisationId == organisationId &&
                a.AppointmentProfileId == appointmentProfileId &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                a.SlotStart < endOfDay &&
                startOfDay < a.SlotEnd)
            .OrderBy(a => a.SlotStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentScheduleRule>> GetScheduleRulesAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        CancellationToken cancellationToken)
    {
        return await _context.AppointmentScheduleRules
            .Where(r => r.OrganisationId == organisationId && r.AppointmentProfileId == appointmentProfileId)
            .OrderBy(r => r.DayOfWeek)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertScheduleRulesAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        IReadOnlyList<AppointmentScheduleRule> rules,
        CancellationToken cancellationToken)
    {
        var existing = await _context.AppointmentScheduleRules
            .Where(r => r.OrganisationId == organisationId && r.AppointmentProfileId == appointmentProfileId)
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

    public async Task AddProfileAsync(AppointmentProfile profile, CancellationToken cancellationToken)
    {
        await _context.AppointmentProfiles.AddAsync(profile, cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentProfile>> GetProfilesByOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await _context.AppointmentProfiles
            .Where(p => p.OrganisationId == organisationId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppointmentProfile?> GetProfileByIdAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        CancellationToken cancellationToken)
    {
        return await _context.AppointmentProfiles
            .FirstOrDefaultAsync(
                p => p.OrganisationId == organisationId && p.Id == appointmentProfileId,
                cancellationToken);
    }
}
