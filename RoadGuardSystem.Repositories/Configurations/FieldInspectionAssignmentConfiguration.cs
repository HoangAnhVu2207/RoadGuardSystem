using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class FieldInspectionAssignmentConfiguration : IEntityTypeConfiguration<FieldInspectionAssignment>
{
    public void Configure(EntityTypeBuilder<FieldInspectionAssignment> builder)
    {
        builder.ToTable("FieldInspectionAssignments", table =>
        {
            table.HasCheckConstraint("CK_FieldInspectionAssignments_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_FieldInspectionAssignments_State",
                "([Status] = 1 AND [EndedAt] IS NULL AND [Reason] IS NULL) OR " +
                "([Status] IN (2, 3) AND [EndedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason]))) > 0)");
            table.HasCheckConstraint(
                "CK_FieldInspectionAssignments_TimestampOrder",
                "[EndedAt] IS NULL OR [EndedAt] >= [AssignedAt]");
        });

        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(assignment => assignment.FieldInspectionTaskId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(assignment => assignment.AssignedToUserId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(assignment => assignment.AssignedByUserId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(assignment => assignment.AssignedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(assignment => assignment.EndedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(assignment => assignment.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(assignment => assignment.Reason).HasColumnType("nvarchar(1000)");
        builder.HasIndex(assignment => assignment.FieldInspectionTaskId)
            .IsUnique()
            .HasFilter("[Status] = 1")
            .HasDatabaseName("UX_FieldInspectionAssignments_ActiveTask");
        builder.HasIndex(assignment => assignment.AssignedToUserId)
            .HasDatabaseName("IX_FieldInspectionAssignments_AssignedToUserId");
        builder.HasOne<FieldInspectionTask>()
            .WithMany()
            .HasForeignKey(assignment => assignment.FieldInspectionTaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
