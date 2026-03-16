using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentProfile = NextTurn.Domain.Appointment.Entities.AppointmentProfile;
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
        Guid appointmentProfileId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken cancellationToken);

    Task<bool> HasOverlapExcludingAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        Guid excludedAppointmentId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveAppointmentForUserOnDateAsync(
        Guid organisationId,
        Guid userId,
        DateOnly date,
        Guid? excludedAppointmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentEntity>> GetByOrganisationAndDateAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentScheduleRule>> GetScheduleRulesAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        CancellationToken cancellationToken);

    Task UpsertScheduleRulesAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        IReadOnlyList<AppointmentScheduleRule> rules,
        CancellationToken cancellationToken);

    Task AddProfileAsync(AppointmentProfile profile, CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentProfile>> GetProfilesByOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    Task<AppointmentProfile?> GetProfileByIdAsync(
        Guid organisationId,
        Guid appointmentProfileId,
        CancellationToken cancellationToken);
}
