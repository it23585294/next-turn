using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.Domain.Appointment.Repositories;

/// <summary>
/// Persistence contract for appointment booking operations.
/// </summary>
public interface IAppointmentRepository
{
    Task AddAsync(AppointmentEntity appointment, CancellationToken cancellationToken);

    Task<bool> HasOverlapAsync(
        Guid organisationId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentEntity>> GetByOrganisationAndDateAsync(
        Guid organisationId,
        DateOnly date,
        CancellationToken cancellationToken);
}
