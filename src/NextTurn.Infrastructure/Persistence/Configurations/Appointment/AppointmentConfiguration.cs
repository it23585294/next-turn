using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTurn.Domain.Appointment.Enums;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.Infrastructure.Persistence.Configurations.Appointment;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<AppointmentEntity>
{
    public void Configure(EntityTypeBuilder<AppointmentEntity> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.OrganisationId)
            .IsRequired();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.SlotStart)
            .IsRequired();

        builder.Property(a => a.SlotEnd)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(AppointmentStatus.Confirmed);

        builder.HasIndex(a => new { a.OrganisationId, a.SlotStart })
            .HasDatabaseName("IX_Appointments_OrganisationId_SlotStart");

        builder.HasIndex(a => new { a.OrganisationId, a.SlotStart, a.SlotEnd })
            .HasDatabaseName("UX_Appointments_OrganisationId_SlotStart_SlotEnd_Active")
            .IsUnique()
            .HasFilter("[Status] <> 'Cancelled'");
    }
}
