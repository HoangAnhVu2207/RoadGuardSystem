using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Files;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class FieldTaskStartOriginConfiguration : IEntityTypeConfiguration<FieldTaskStartOrigin>
{
    public void Configure(EntityTypeBuilder<FieldTaskStartOrigin> builder)
    {
        builder.ToTable("FieldTaskStartOrigins", table => { table.HasTrigger("TR_FieldTaskStartOrigins_Immutable"); table.HasTrigger("TR_FieldTaskStartOrigins_Scope"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OriginalActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey(x => x.SegmentSetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PavementLayoutRevision>().WithMany().HasForeignKey(x => x.LayoutRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GeometryMapPublication>().WithMany().HasForeignKey(x => x.MapPublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CrsProfileRevision>().WithMany().HasForeignKey(x => x.CrsProfileRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.SlabId).HasMaxLength(160);
        builder.Property(x => x.LocationPolicyVersion).HasMaxLength(60);
        builder.HasOne<FieldInspectionOperationOrigin>().WithMany().HasForeignKey(x => x.OperationOriginId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TaskId).IsUnique();
        builder.HasIndex(x => new { x.ProjectId, x.OriginId }).IsUnique();
        builder.Property(x => x.ContentHash).HasMaxLength(64);
        builder.Property(x => x.OperationKind).HasMaxLength(40);
        builder.Property(x => x.TimeProvenance).HasMaxLength(40);
        builder.Property(x => x.BootId).HasMaxLength(200);
    }
}

public sealed class FieldInspectionSubmissionConfiguration : IEntityTypeConfiguration<FieldInspectionSubmission>
{
    public void Configure(EntityTypeBuilder<FieldInspectionSubmission> builder)
    {
        builder.ToTable("FieldInspectionSubmissions", table => { table.HasTrigger("TR_FieldInspectionSubmissions_Immutable"); table.HasTrigger("TR_FieldInspectionSubmissions_Scope"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionSubmission>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldTaskStartOrigin>().WithMany().HasForeignKey(x => x.StartOriginId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OriginalActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionOperationOrigin>().WithMany().HasForeignKey(x => x.OperationOriginId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RootId, x.Revision }).IsUnique();
        builder.HasIndex(x => x.SessionId).IsUnique().HasDatabaseName("UX_FieldInspectionSubmissions_Session");
        builder.HasIndex(x => x.TaskId).IsUnique().HasFilter("[Revision] = 1").HasDatabaseName("UX_FieldInspectionSubmissions_TaskRoot");
        builder.HasIndex(x => new { x.ProjectId, x.OriginId }).IsUnique();
        builder.Property(x => x.ContentHash).HasMaxLength(64);
        builder.Property(x => x.Readiness).HasMaxLength(20);
    }
}

public sealed class FieldInspectionReviewConfiguration : IEntityTypeConfiguration<FieldInspectionReview>
{
    public void Configure(EntityTypeBuilder<FieldInspectionReview> builder)
    {
        builder.ToTable("FieldInspectionReviews", table => { table.HasTrigger("TR_FieldInspectionReviews_Immutable"); table.HasTrigger("TR_FieldInspectionReviews_Scope"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Decision).HasMaxLength(20);
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.ReceiptActivation).HasMaxLength(60);
    }
}

public sealed class FieldInspectionEvidenceLinkConfiguration : IEntityTypeConfiguration<FieldInspectionEvidenceLink>
{
    public void Configure(EntityTypeBuilder<FieldInspectionEvidenceLink> builder)
    {
        builder.ToTable("FieldInspectionEvidenceLinks", table => { table.HasTrigger("TR_FieldInspectionEvidenceLinks_Immutable"); table.HasTrigger("TR_FieldInspectionEvidenceLinks_Scope"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SubmissionId, x.CaptureOriginId }).IsUnique();
        builder.Property(x => x.Purpose).HasMaxLength(20);
        builder.Property(x => x.DeclaredChecksum).HasMaxLength(64);
        builder.Property(x => x.MediaType).HasMaxLength(120);
    }
}

public sealed class FieldInspectionEvidenceReuseDecisionConfiguration : IEntityTypeConfiguration<FieldInspectionEvidenceReuseDecision>
{
    public void Configure(EntityTypeBuilder<FieldInspectionEvidenceReuseDecision> builder)
    {
        builder.ToTable("FieldInspectionEvidenceReuseDecisions", table => { table.HasTrigger("TR_FieldInspectionEvidenceReuseDecisions_Immutable"); table.HasTrigger("TR_FieldInspectionEvidenceReuseDecisions_Scope"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.SourceKind).HasMaxLength(20);
        builder.Property(x => x.FileChecksum).HasMaxLength(64);
        builder.Property(x => x.Reason).HasMaxLength(2000);
    }
}

public sealed class FieldInspectionTaskEventConfiguration : IEntityTypeConfiguration<FieldInspectionTaskEvent>
{
    public void Configure(EntityTypeBuilder<FieldInspectionTaskEvent> builder)
    {
        builder.ToTable("FieldInspectionTaskEvents", table => { table.HasTrigger("TR_FieldInspectionTaskEvents_Immutable"); table.HasTrigger("TR_FieldInspectionTaskEvents_Scope"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.FactsJson).HasDefaultValue("{}");
        builder.HasOne<GeometryLocationImpact>().WithMany().HasForeignKey(x => x.LocationImpactId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GeometryLocationImpactDecision>().WithMany().HasForeignKey(x => x.LocationImpactDecisionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Kind).HasMaxLength(20);
        builder.Property(x => x.Reason).HasMaxLength(2000);
    }
}

public sealed class FieldInspectionLocationProofConfiguration : IEntityTypeConfiguration<FieldInspectionLocationProof>
{
    public void Configure(EntityTypeBuilder<FieldInspectionLocationProof> builder)
    {
        builder.ToTable("FieldInspectionLocationProofs", table => { table.HasTrigger("TR_FieldInspectionLocationProofs_Immutable"); table.HasTrigger("TR_FieldInspectionLocationProofs_Scope"); });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Kind).HasMaxLength(30);
        builder.Property(x => x.VerificationState).HasMaxLength(20);
    }
}

public sealed class FieldInspectionOperationOriginConfiguration : IEntityTypeConfiguration<FieldInspectionOperationOrigin>
{
    public void Configure(EntityTypeBuilder<FieldInspectionOperationOrigin> builder)
    {
        builder.ToTable("FieldInspectionOperationOrigins", table =>
        {
            table.HasTrigger("TR_FieldInspectionOperationOrigins_Immutable");
            table.HasTrigger("TR_FieldInspectionOperationOrigins_Scope");
            table.HasCheckConstraint("CK_FieldInspectionOperationOrigins_Kind", "[Kind] IN ('FIELD_START','FIELD_SUBMISSION','FIELD_ACCEPT','REPAIR_ASSESSMENT','REPAIR_EXECUTION_START','REPAIR_EXECUTION_FINISH') AND [SchemaVersion]=1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.ProjectId, x.OriginId }).IsUnique();
        builder.Property(x => x.Kind).HasMaxLength(40);
        builder.Property(x => x.ContentHash).HasMaxLength(64);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OriginalActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
