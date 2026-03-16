using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueueStaffAssignment = NextTurn.Domain.Queue.Entities.QueueStaffAssignment;

namespace NextTurn.Infrastructure.Persistence.Configurations.Queue;

public sealed class QueueStaffAssignmentConfiguration : IEntityTypeConfiguration<QueueStaffAssignment>
{
    public void Configure(EntityTypeBuilder<QueueStaffAssignment> builder)
    {
        builder.ToTable("QueueStaffAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.OrganisationId)
            .IsRequired();

        builder.Property(a => a.QueueId)
            .IsRequired();

        builder.Property(a => a.StaffUserId)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasIndex(a => a.OrganisationId)
            .HasDatabaseName("IX_QueueStaffAssignments_OrganisationId");

        builder.HasIndex(a => new { a.QueueId, a.StaffUserId })
            .IsUnique()
            .HasDatabaseName("UX_QueueStaffAssignments_QueueId_StaffUserId");
    }
}
