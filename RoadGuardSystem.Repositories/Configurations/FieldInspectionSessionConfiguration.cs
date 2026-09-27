using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class FieldInspectionSessionConfiguration : IEntityTypeConfiguration<FieldInspectionSession>
{
    public void Configure(EntityTypeBuilder<FieldInspectionSession> builder)
    {
        builder.ToTable("FieldInspectionSessions", table =>
        {
            table.HasTrigger("TR_FieldInspectionSessions_Integrity");
            table.HasTrigger("TR_FieldInspectionSessions_Immutable");
            table.HasCheckConstraint("CK_FieldInspectionSessions_Purpose", "[Purpose] IN (1, 2)");
            table.HasCheckConstraint("CK_FieldInspectionSessions_Status", "[Status] IN (1, 2, 3, 4)");
            table.HasCheckConstraint(
                "CK_FieldInspectionSessions_PurposeScope",
                "([Purpose] = 1 AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL) OR " +
                "([Purpose] = 2 AND [FieldInspectionTaskId] IS NULL)");
        });

        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(session => session.Purpose).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(session => session.FieldInspectionTaskId).HasColumnType("uniqueidentifier");
        builder.Property(session => session.ProjectId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(session => session.RoadSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(session => session.SurveyId).HasColumnType("uniqueidentifier");
        builder.Property(session => session.SessionCode).HasColumnType("nvarchar(80)").IsRequired();
        builder.Property(session => session.InspectorUserId).HasColumnType("uniqueidentifier");
        builder.Property(session => session.InspectorName).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(session => session.ConductedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(session => session.WeatherCondition).HasColumnType("nvarchar(100)");
        builder.Property(session => session.Method).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(session => session.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(session => session.EvidenceFileId).HasColumnType("uniqueidentifier");
        builder.HasIndex(session => session.SessionCode).IsUnique().HasDatabaseName("UX_FieldInspectionSessions_SessionCode");
        builder.HasIndex(session => new { session.ProjectId, session.RoadSectionVersionId }).HasDatabaseName("IX_FieldInspectionSessions_ProjectVersion");
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(session => session.FieldInspectionTaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(session => session.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(session => session.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Survey>().WithMany().HasForeignKey(session => session.SurveyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(session => session.InspectorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(session => session.EvidenceFileId).OnDelete(DeleteBehavior.Restrict);
    }
}
