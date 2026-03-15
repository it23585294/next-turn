using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppointmentScheduleRule = NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule;

namespace NextTurn.Infrastructure.Persistence.Configurations.Appointment;

public sealed class AppointmentScheduleRuleConfiguration : IEntityTypeConfiguration<AppointmentScheduleRule>
{
    public void Configure(EntityTypeBuilder<AppointmentScheduleRule> builder)
    {
        builder.ToTable("AppointmentScheduleRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.OrganisationId)
            .IsRequired();

        builder.Property(r => r.DayOfWeek)
            .IsRequired();

        builder.Property(r => r.IsEnabled)
            .IsRequired();

        builder.Property(r => r.StartTime)
            .IsRequired();

        builder.Property(r => r.EndTime)
            .IsRequired();

        builder.Property(r => r.SlotDurationMinutes)
            .IsRequired();

        builder.HasIndex(r => new { r.OrganisationId, r.DayOfWeek })
            .HasDatabaseName("UX_AppointmentScheduleRules_OrganisationId_DayOfWeek")
            .IsUnique();
    }
}