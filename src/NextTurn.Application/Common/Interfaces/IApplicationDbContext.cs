using Microsoft.EntityFrameworkCore;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using AppointmentProfile = NextTurn.Domain.Appointment.Entities.AppointmentProfile;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;
using NextTurn.Domain.Auth.Entities;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using QueueEntity        = NextTurn.Domain.Queue.Entities.Queue;
using QueueEntry         = NextTurn.Domain.Queue.Entities.QueueEntry;

namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ApplicationDbContext exposed to the Application layer.
/// Application handlers depend on this interface, never on the concrete DbContext.
/// This keeps the Application layer free of direct EF Core coupling.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User>             Users        { get; }
    DbSet<OrganisationEntity> Organisations { get; }
    DbSet<QueueEntity>      Queues       { get; }
    DbSet<QueueEntry>       QueueEntries { get; }
    DbSet<AppointmentEntity> Appointments { get; }
    DbSet<AppointmentProfile> AppointmentProfiles { get; }
    DbSet<AppointmentScheduleRule> AppointmentScheduleRules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
