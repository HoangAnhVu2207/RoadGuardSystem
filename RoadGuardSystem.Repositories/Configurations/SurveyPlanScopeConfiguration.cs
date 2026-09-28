using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyPlanScopeConfiguration : IEntityTypeConfiguration<SurveyPlanScope>
{
    public void Configure(EntityTypeBuilder<SurveyPlanScope> builder)
    {
        builder.ToTable("SurveyPlanScopes", table =>
            table.HasCheckConstraint("CK_SurveyPlanScopes_SegmentIdsJson", "ISJSON([SegmentIdsJson]) = 1"));
        builder.HasKey(scope => scope.Id);
        builder.Property(scope => scope.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(scope => scope.SurveyPlanId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(scope => scope.RouteSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(scope => scope.SegmentSetId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(scope => scope.SegmentIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(scope => scope.TargetBand).HasColumnType("nvarchar(32)").IsRequired();
        builder.HasIndex(scope => new { scope.SurveyPlanId, scope.RouteSectionVersionId, scope.SegmentSetId, scope.TargetBand })
            .IsUnique()
            .HasDatabaseName("UX_SurveyPlanScopes_UniqueBand");
        builder.HasOne<SurveyPlan>().WithMany().HasForeignKey(scope => scope.SurveyPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(scope => scope.RouteSectionVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
