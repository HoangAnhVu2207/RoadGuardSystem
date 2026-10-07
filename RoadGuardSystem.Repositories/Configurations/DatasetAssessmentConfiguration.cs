using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DatasetAssessmentConfiguration : IEntityTypeConfiguration<DatasetAssessment>
{
    public void Configure(EntityTypeBuilder<DatasetAssessment> builder)
    {
        var b = builder;
        b.ToTable("DatasetAssessments", t => t.HasTrigger("TR_DatasetAssessments_Immutable"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.MethodVersion).HasMaxLength(80); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<SurveyDataVersion>().WithMany().HasForeignKey(x => x.DatasetId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DatasetId, x.ReviewedAt });
    }
}
public sealed class DatasetAssessmentItemConfiguration : IEntityTypeConfiguration<DatasetAssessmentItem>
{
    public void Configure(EntityTypeBuilder<DatasetAssessmentItem> builder)
    {
        var b = builder;
        b.ToTable("DatasetAssessmentItems", t =>
        {
            t.HasTrigger("TR_DatasetAssessmentItems_Immutable");
            t.HasCheckConstraint("CK_Assessment_Status", "[PositionStatus] IN ('PASS','FAIL','UNKNOWN') AND [QualityStatus] IN ('PASS','FAIL','UNKNOWN') AND [CoverageStatus] IN ('PASS','FAIL','UNKNOWN')");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TargetBand).HasMaxLength(20); b.Property(x => x.PositionStatus).HasMaxLength(10);
        b.Property(x => x.QualityStatus).HasMaxLength(10); b.Property(x => x.CoverageStatus).HasMaxLength(10);
        b.HasIndex(x => new { x.AssessmentId, x.RouteVersionId, x.SegmentSetId, x.SegmentId, x.TargetBand }).IsUnique();
        b.HasOne<DatasetAssessment>().WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RoadSegment>().WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class BaselineSelectionConfiguration : IEntityTypeConfiguration<BaselineSelection>
{
    public void Configure(EntityTypeBuilder<BaselineSelection> builder)
    {
        var b = builder;
        b.ToTable("BaselineSelections", t => t.HasTrigger("TR_BaselineSelections_Immutable"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SelectedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class BaselineSelectionItemConfiguration : IEntityTypeConfiguration<BaselineSelectionItem>
{
    public void Configure(EntityTypeBuilder<BaselineSelectionItem> builder)
    {
        var b = builder;
        b.ToTable("BaselineSelectionItems", t => t.HasTrigger("TR_BaselineSelectionItems_Immutable"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.TargetBand).HasMaxLength(20);
        b.HasOne<BaselineSelection>().WithMany().HasForeignKey(x => x.BaselineSelectionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DatasetAssessment>().WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SurveyDataVersion>().WithMany().HasForeignKey(x => x.DatasetId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BaselineSelectionId, x.RouteVersionId, x.SegmentSetId, x.SegmentId, x.TargetBand }).IsUnique();
    }
}
public sealed class BaselineCurrentPointerConfiguration : IEntityTypeConfiguration<BaselineCurrentPointer>
{
    public void Configure(EntityTypeBuilder<BaselineCurrentPointer> builder)
    {
        var b = builder;
        b.ToTable("BaselineCurrentPointers"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TargetBand).HasMaxLength(20); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.ProjectId, x.RouteVersionId, x.SegmentSetId, x.SegmentId, x.TargetBand }).IsUnique();
        b.HasOne<BaselineSelectionItem>().WithMany().HasForeignKey(x => x.SelectionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }
}
