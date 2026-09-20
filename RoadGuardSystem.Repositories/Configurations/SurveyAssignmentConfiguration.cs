using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyAssignmentConfiguration : IEntityTypeConfiguration<SurveyAssignment>
{
    public void Configure(EntityTypeBuilder<SurveyAssignment> builder)
    {
        builder.ToTable("SurveyAssignments", table =>
        {
            table.HasCheckConstraint(
                "CK_SurveyAssignments_Rejection",
                "([RejectedAt] IS NULL AND [RejectionReason] IS NULL) " +
                "OR ([RejectedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([RejectionReason]))) > 0 AND [EndedAt] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_SurveyAssignments_AcceptanceRejection",
                "[AcceptedAt] IS NULL OR [RejectedAt] IS NULL");
            table.HasCheckConstraint(
                "CK_SurveyAssignments_TimestampOrder",
                "([AcceptedAt] IS NULL OR [AcceptedAt] >= [AssignedAt]) " +
                "AND ([RejectedAt] IS NULL OR [RejectedAt] >= [AssignedAt]) " +
                "AND ([EndedAt] IS NULL OR [EndedAt] >= [AssignedAt])");
            table.HasCheckConstraint(
                "CK_SurveyAssignments_ActiveReassignmentReason",
                "[EndedAt] IS NOT NULL OR [ReassignmentReason] IS NULL");
            table.HasCheckConstraint(
                "CK_SurveyAssignments_ReassignmentNotRejected",
                "[ReassignmentReason] IS NULL OR [RejectedAt] IS NULL");
        });

        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(assignment => assignment.SurveyRequestId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(assignment => assignment.OperatorUserId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(assignment => assignment.AssignedByUserId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(assignment => assignment.AssignedAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();
        builder.Property(assignment => assignment.AcceptedAt)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(assignment => assignment.RejectedAt)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(assignment => assignment.RejectionReason)
            .HasColumnType("nvarchar(1000)");
        builder.Property(assignment => assignment.ReassignmentReason)
            .HasColumnType("nvarchar(1000)");
        builder.Property(assignment => assignment.EndedAt)
            .HasColumnType("datetimeoffset(7)");
        builder.HasIndex(assignment => new { assignment.SurveyRequestId, assignment.EndedAt })
            .HasDatabaseName("IX_SurveyAssignments_RequestEndedAt");
        builder.HasIndex(assignment => assignment.SurveyRequestId)
            .IsUnique()
            .HasFilter("[EndedAt] IS NULL")
            .HasDatabaseName("UX_SurveyAssignments_ActiveRequest");
        builder.HasOne<SurveyRequest>()
            .WithMany()
            .HasForeignKey(assignment => assignment.SurveyRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(assignment => assignment.OperatorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
