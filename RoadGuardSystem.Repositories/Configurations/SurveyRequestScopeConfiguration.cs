using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyRequestScopeConfiguration : IEntityTypeConfiguration<SurveyRequestScope>
{
    public void Configure(EntityTypeBuilder<SurveyRequestScope> builder)
    {
        builder.ToTable("SurveyRequestScopes", table =>
            table.HasCheckConstraint("CK_SurveyRequestScopes_SegmentIdsJson", "ISJSON([SegmentIdsJson]) = 1"));
        builder.HasKey(scope => scope.Id);
        builder.Property(scope => scope.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(scope => scope.SurveyRequestId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(scope => scope.RouteSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(scope => scope.SegmentSetId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(scope => scope.SegmentIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(scope => scope.TargetBand).HasColumnType("nvarchar(32)").IsRequired();
        builder.HasIndex(scope => new { scope.SurveyRequestId, scope.RouteSectionVersionId, scope.SegmentSetId, scope.TargetBand })
            .IsUnique()
            .HasDatabaseName("UX_SurveyRequestScopes_UniqueBand");
        builder.HasOne<SurveyRequest>().WithMany().HasForeignKey(scope => scope.SurveyRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(scope => scope.RouteSectionVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
