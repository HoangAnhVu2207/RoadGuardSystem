using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyPlanConfiguration : IEntityTypeConfiguration<SurveyPlan>
{
    public void Configure(EntityTypeBuilder<SurveyPlan> builder)
    {
        builder.ToTable("SurveyPlans", table =>
        {
            table.HasTrigger("TR_SurveyPlans_ScopeIntegrity");
            table.HasCheckConstraint(
                "CK_SurveyPlans_PlannedDateRange",
                "[PlannedEndAt] >= [PlannedStartAt]");
            table.HasCheckConstraint("CK_SurveyPlans_SurveyType", "[SurveyType] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_SurveyPlans_Status", "[Status] IN (1, 2, 3, 4, 5)");
        });

        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(plan => plan.ProjectId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(plan => plan.RoadSectionId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(plan => plan.PlannedStartAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();
        builder.Property(plan => plan.PlannedEndAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();
        builder.Property(plan => plan.SurveyType)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(plan => plan.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.HasIndex(plan => new { plan.ProjectId, plan.RoadSectionId, plan.PlannedStartAt })
            .HasDatabaseName("IX_SurveyPlans_ProjectRoadStart");
        builder.HasIndex(plan => new { plan.ProjectId, plan.RoadSectionId, plan.SurveyType })
            .IsUnique()
            .HasFilter("[Status] IN (1, 2, 3)")
            .HasDatabaseName("UX_SurveyPlans_ActiveScope");
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(plan => plan.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSection>()
            .WithMany()
            .HasForeignKey(plan => plan.RoadSectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
