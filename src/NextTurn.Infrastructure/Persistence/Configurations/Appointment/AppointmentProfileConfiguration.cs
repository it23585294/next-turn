using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppointmentProfile = NextTurn.Domain.Appointment.Entities.AppointmentProfile;

namespace NextTurn.Infrastructure.Persistence.Configurations.Appointment;

public sealed class AppointmentProfileConfiguration : IEntityTypeConfiguration<AppointmentProfile>
{
    public void Configure(EntityTypeBuilder<AppointmentProfile> builder)
    {
        builder.ToTable("AppointmentProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.OrganisationId)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.ShareableLink)
            .HasMaxLength(300)
            .IsRequired();

        builder.HasIndex(p => new { p.OrganisationId, p.Name })
            .HasDatabaseName("UX_AppointmentProfiles_OrganisationId_Name")
            .IsUnique();

        builder.HasIndex(p => p.OrganisationId)
            .HasDatabaseName("IX_AppointmentProfiles_OrganisationId");
    }
}
