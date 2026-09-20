using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("Surveys", table =>
        {
            table.HasTrigger("TR_Surveys_ScopeIntegrity");
            table.HasCheckConstraint("CK_Surveys_SurveyType", "[SurveyType] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_Surveys_Status", "[Status] IN (1, 2, 3, 4, 5)");
            table.HasCheckConstraint(
                "CK_Surveys_BaselineConfirmation",
                "([IsBaselineConfirmed] = 0 AND [BaselineConfirmedByUserId] IS NULL AND [BaselineConfirmedAt] IS NULL) " +
                "OR ([IsBaselineConfirmed] = 1 AND [BaselineConfirmedByUserId] IS NOT NULL AND [BaselineConfirmedAt] IS NOT NULL)");
        });

        builder.HasKey(survey => survey.Id);
        builder.Property(survey => survey.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(survey => survey.SurveyRequestId)
            .HasColumnType("uniqueidentifier");
        builder.Property(survey => survey.ProjectId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(survey => survey.RoadSectionVersionId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(survey => survey.SurveyType)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(survey => survey.IsBaselineConfirmed)
            .HasColumnType("bit")
            .IsRequired();
        builder.Property(survey => survey.BaselineConfirmedByUserId)
            .HasColumnType("uniqueidentifier");
        builder.Property(survey => survey.BaselineConfirmedAt)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(survey => survey.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.HasIndex(survey => new { survey.ProjectId, survey.Status })
            .HasDatabaseName("IX_Surveys_ProjectStatus");
        builder.HasIndex(survey => survey.RoadSectionVersionId)
            .HasDatabaseName("IX_Surveys_RoadSectionVersionId");
        builder.HasOne<SurveyRequest>()
            .WithMany()
            .HasForeignKey(survey => survey.SurveyRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(survey => survey.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>()
            .WithMany()
            .HasForeignKey(survey => survey.RoadSectionVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(survey => survey.BaselineConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
