using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RoadGeometryDraftConfiguration : IEntityTypeConfiguration<RoadGeometryDraft>
{
    public void Configure(EntityTypeBuilder<RoadGeometryDraft> builder)
    {
        var b = builder;
        b.ToTable("RoadGeometryDrafts", table => table.HasCheckConstraint("CK_RoadGeometryDrafts_Submission",
            "([SubmittedAt] IS NULL AND [SubmittedBy] IS NULL AND [SubmittedContentVersion] IS NULL AND [Status] NOT IN ('SUBMITTED', 'REJECTED')) OR ([SubmittedAt] IS NOT NULL AND [SubmittedBy] IS NOT NULL AND [SubmittedContentVersion] IS NOT NULL AND [Status] IN ('SUBMITTED', 'CONFIRMED', 'REJECTED'))"));
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.ProjectId);
        b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.RoadCode).HasMaxLength(80);
        b.Property(x => x.RoadName).HasMaxLength(255); b.Property(x => x.Status).HasMaxLength(16);
        b.Property(x => x.SubmittedContentVersion).HasMaxLength(16);
        b.HasIndex(x => x.ProjectId, "UX_RoadGeometryDrafts_ProjectSubmission")
            .IsUnique().HasFilter("[SubmittedAt] IS NOT NULL");
        b.Property(x => x.SourceChecksum).HasMaxLength(64); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RoadSection>().WithMany().HasForeignKey(x => x.RoadSectionId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class GeometrySubmissionRoundConfiguration : IEntityTypeConfiguration<GeometrySubmissionRound>
{
    public void Configure(EntityTypeBuilder<GeometrySubmissionRound> builder)
    {
        builder.ToTable("GeometrySubmissionRounds");
        builder.HasKey(x => new { x.DraftId, x.Number });
        builder.HasOne<RoadGeometryDraft>().WithMany().HasForeignKey(x => x.DraftId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.ContentVersion).HasMaxLength(16);
        builder.Property(x => x.ContentFingerprint).HasMaxLength(64);
        builder.Property(x => x.Outcome).HasMaxLength(16);
        builder.Property(x => x.RejectionReason).HasMaxLength(2000);
    }
}

public sealed class RoadGeometryMetadataConfiguration : IEntityTypeConfiguration<RoadGeometryMetadata>
{
    public void Configure(EntityTypeBuilder<RoadGeometryMetadata> builder)
    {
        var b = builder;
        b.ToTable("RoadGeometryMetadata"); b.HasKey(x => x.RoadSectionVersionId);
        b.Property(x => x.GeometryHash).HasMaxLength(64);
        b.HasOne<RoadSectionVersion>().WithOne().HasForeignKey<RoadGeometryMetadata>(x => x.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RoadGeometryDraft>().WithMany().HasForeignKey(x => x.SourceDraftId).OnDelete(DeleteBehavior.Restrict);
    }
}
