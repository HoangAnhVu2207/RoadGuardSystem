using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class HuyTrainingLabelHeadConfiguration : IEntityTypeConfiguration<HuyTrainingLabelHead>
{
    public void Configure(EntityTypeBuilder<HuyTrainingLabelHead> builder)
    {
        builder.ToTable("TrainingLabels", table => table.UseSqlOutputClause(false).HasCheckConstraint("CK_TrainingLabels_Source",
            "([SourceKind]='REPORT' AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR " +
            "([SourceKind]='AI_DETECTION' AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)"));
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.SourceKind).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(row => row.RowVersion).IsRowVersion();
        builder.HasIndex(row => new { row.ProjectId, row.SourceKind, row.SourceId });
        builder.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Report>().WithMany().HasForeignKey(row => row.ReportSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIDetection>().WithMany().HasForeignKey(row => row.AIDetectionSourceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyTrainingLabelRevisionConfiguration : IEntityTypeConfiguration<HuyTrainingLabelRevision>
{
    public void Configure(EntityTypeBuilder<HuyTrainingLabelRevision> builder)
    {
        builder.ToTable("TrainingLabelRevisions", table => table.UseSqlOutputClause(false).HasCheckConstraint("CK_TrainingLabelRevisions_Bbox",
            "[X]>=0 AND [Y]>=0 AND [Width]>0 AND [Height]>0 AND [X]+[Width]<=1 AND [Y]+[Height]<=1"));
        builder.HasKey(row => row.Id);
        builder.HasAlternateKey(row => new { row.Id, row.LabelId });
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.SourceVersion).HasMaxLength(200).IsRequired();
        builder.Property(row => row.FileVersion).HasMaxLength(200).IsRequired();
        builder.Property(row => row.DefectTypeCode).HasMaxLength(80).IsUnicode(false).IsRequired();
        builder.Property(row => row.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(row => row.X).HasPrecision(9, 7);
        builder.Property(row => row.Y).HasPrecision(9, 7);
        builder.Property(row => row.Width).HasPrecision(9, 7);
        builder.Property(row => row.Height).HasPrecision(9, 7);
        builder.HasIndex(row => new { row.LabelId, row.Revision }).IsUnique();
        builder.HasOne<HuyTrainingLabelHead>().WithMany().HasForeignKey(row => row.LabelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(row => row.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DefectType>().WithMany().HasForeignKey(row => row.DefectTypeCode).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyTrainingLabelReviewConfiguration : IEntityTypeConfiguration<HuyTrainingLabelReview>
{
    public void Configure(EntityTypeBuilder<HuyTrainingLabelReview> builder)
    {
        builder.ToTable("TrainingLabelReviews", table => table.UseSqlOutputClause(false).HasCheckConstraint("CK_TrainingLabelReviews_Decision",
            "[Decision] IN ('APPROVED','REJECTED')"));
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.Decision).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(row => row.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(row => row.RevisionId).IsUnique();
        builder.HasOne<HuyTrainingLabelRevision>().WithMany()
            .HasForeignKey(row => new { row.RevisionId, row.LabelId })
            .HasPrincipalKey(row => new { row.Id, row.LabelId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
