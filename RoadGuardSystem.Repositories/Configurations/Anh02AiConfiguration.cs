using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Catalogs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class Anh02AiConfiguration : IEntityTypeConfiguration<AiMockRun>, IEntityTypeConfiguration<AiResultProvenance>,
    IEntityTypeConfiguration<AiDetectionProvenance>, IEntityTypeConfiguration<AiManifestFileReference>
{
    public void Configure(EntityTypeBuilder<AiMockRun> builder)
    {
        builder.ToTable("Anh02AiMockRuns", table =>
        {
            table.HasTrigger("TR_Anh02AiMockRuns_Identity");
            table.HasCheckConstraint("CK_Anh02AiMockRuns_Stage", "[Stage] IN ('VIDEO_ANALYSIS','DUPLICATE_MATCHING')");
            table.HasCheckConstraint("CK_Anh02AiMockRuns_Status", "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')");
            table.HasCheckConstraint("CK_Anh02AiMockRuns_Manifest", "ISJSON([CanonicalManifest])=1");
            table.HasCheckConstraint("CK_Anh02AiMockRuns_Completion", "([Status]='SUCCEEDED' AND [ResultId] IS NOT NULL AND [CompletedAt] IS NOT NULL) OR ([Status]<>'SUCCEEDED' AND [ResultId] IS NULL)");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RowVersion).IsRowVersion();
        foreach (var name in new[] { "Stage", "Status" }) builder.Property<string>(name).HasMaxLength(24).IsUnicode(false);
        foreach (var name in new[] { "FixtureVersion", "GeometryVersion" }) builder.Property<string>(name).HasMaxLength(128).IsUnicode(false);
        builder.Property(x => x.ManifestHash).HasMaxLength(64).IsUnicode(false);
        builder.Property(x => x.ErrorCode).HasMaxLength(80).IsUnicode(false);
        builder.HasIndex(x => x.ProcessingJobId).IsUnique().HasFilter("[Stage]='VIDEO_ANALYSIS'");
        builder.HasIndex(x => new { x.Status, x.LeaseUntil });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SurveyDataVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingJob>().WithMany().HasForeignKey(x => x.ProcessingJobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIModelVersion>().WithMany().HasForeignKey(x => x.ModelVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey(x => new { x.SegmentSetId, x.RouteVersionId })
            .HasPrincipalKey(x => new { x.Id, x.RoadSectionVersionId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiMockRun>().WithMany().HasForeignKey(x => x.AnalysisRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiResultProvenance>().WithMany().HasForeignKey(x => new { x.ResultId, x.Id })
            .HasPrincipalKey(x => new { x.Id, x.RunId }).OnDelete(DeleteBehavior.Restrict);
        foreach (var name in new[] { "ProjectId", "DatasetVersionId", "ProcessingJobId", "AttemptId", "ModelVersionId", "RouteVersionId",
            "SegmentSetId", "CreatedBy", "CreatedAt", "Stage", "FixtureVersion", "GeometryVersion", "ManifestHash", "CanonicalManifest", "AnalysisRunId" })
            builder.Property(name).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
    public void Configure(EntityTypeBuilder<AiResultProvenance> builder)
    {
        builder.ToTable("Anh02AiResultProvenance", table => { table.HasTrigger("TR_Anh02AiResultProvenance_Immutable"); table.HasCheckConstraint("CK_Anh02AiResult_Json", "ISJSON([CanonicalResult])=1"); });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => x.RunId).IsUnique();
        builder.Property(x => x.ManifestHash).HasMaxLength(64).IsUnicode(false);
        builder.Property(x => x.ResultHash).HasMaxLength(64).IsUnicode(false);
        builder.HasOne<AiMockRun>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingJob>().WithMany().HasForeignKey(x => x.ProcessingJobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<AiDetectionProvenance> builder)
    {
        builder.ToTable("Anh02AiDetectionProvenance", table =>
        {
            table.HasTrigger("TR_Anh02AiDetectionProvenance_Immutable");
            table.HasCheckConstraint("CK_Anh02AiDetection_Time", "[TimestampMilliseconds]>=0 AND [SourceDurationMilliseconds]>0 AND [TimestampMilliseconds]<[SourceDurationMilliseconds]");
            table.HasCheckConstraint("CK_Anh02AiDetection_Box", "[BoxX]>=0 AND [BoxY]>=0 AND [BoxWidth]>0 AND [BoxHeight]>0 AND [BoxX]+[BoxWidth]<=1 AND [BoxY]+[BoxHeight]<=1");
        });
        builder.HasKey(x => x.DetectionId); builder.Property(x => x.DetectionId).ValueGeneratedNever();
        foreach (var name in new[] { "BoxX", "BoxY", "BoxWidth", "BoxHeight" }) builder.Property<decimal>(name).HasPrecision(12, 9);
        foreach (var name in new[] { "SourceVideoFileVersion", "FrameFileVersion", "DerivationHash" }) builder.Property<string>(name).HasMaxLength(128).IsUnicode(false);
        builder.HasOne<AIDetection>().WithMany().HasForeignKey(x => x.DetectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiMockRun>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiResultProvenance>().WithMany().HasForeignKey(x => new { x.ResultId, x.RunId })
            .HasPrincipalKey(x => new { x.Id, x.RunId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiManifestFileReference>().WithMany().HasForeignKey(x => new { x.RunId, x.SourceVideoFileId, x.SourceVideoFileVersion })
            .HasPrincipalKey(x => new { x.RunId, x.FileId, x.FileVersion }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.SourceVideoFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FrameFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSegment>().WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<AiManifestFileReference> builder)
    {
        builder.ToTable("Anh02AiManifestFiles", table => table.HasTrigger("TR_Anh02AiManifestFiles_Immutable"));
        builder.HasKey(x => new { x.RunId, x.FileId });
        builder.Property(x => x.FileVersion).HasMaxLength(128).IsUnicode(false);
        builder.HasOne<AiMockRun>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
