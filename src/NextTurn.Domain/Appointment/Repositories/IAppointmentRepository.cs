using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;

namespace NextTurn.Domain.Appointment.Repositories;

/// <summary>
/// Persistence contract for appointment booking operations.
/// </summary>
public interface IAppointmentRepository
{
    Task AddAsync(AppointmentEntity appointment, CancellationToken cancellationToken);

    Task<AppointmentEntity?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task UpdateAsync(AppointmentEntity appointment, CancellationToken cancellationToken);

    Task<bool> HasOverlapAsync(
        Guid organisationId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken cancellationToken);

    Task<bool> HasOverlapExcludingAsync(
        Guid organisationId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        Guid excludedAppointmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentEntity>> GetByOrganisationAndDateAsync(
        Guid organisationId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentScheduleRule>> GetScheduleRulesAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    Task UpsertScheduleRulesAsync(
        Guid organisationId,
        IReadOnlyList<AppointmentScheduleRule> rules,
        CancellationToken cancellationToken);
}
