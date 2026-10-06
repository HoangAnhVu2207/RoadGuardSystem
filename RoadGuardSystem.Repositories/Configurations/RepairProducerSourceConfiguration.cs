using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Warranties;

namespace RoadGuardSystem.Repositories.Configurations;

internal static class RepairProducerMapping
{
    internal static void Source<T>(EntityTypeBuilder<T> builder, string table) where T : class
    {
        builder.ToTable(table, tableBuilder => tableBuilder.HasTrigger("TR_" + table + "_Immutable"));
        builder.HasKey("Id"); builder.Property<Guid>("Id").ValueGeneratedNever();
        Reference<T, Project>(builder, "ProjectId");
    }
    internal static void Reference<T, TPrincipal>(EntityTypeBuilder<T> builder, string property)
        where T : class where TPrincipal : class => builder.HasOne<TPrincipal>().WithMany()
            .HasForeignKey(property).OnDelete(DeleteBehavior.Restrict);
    internal static void Text<T>(EntityTypeBuilder<T> builder, string property, int length) where T : class
        => builder.Property<string>(property).HasMaxLength(length);
}

public sealed class RepairFieldTaskBindingConfiguration : IEntityTypeConfiguration<RepairFieldTaskBinding>
{
    public void Configure(EntityTypeBuilder<RepairFieldTaskBinding> builder)
    {
        RepairProducerMapping.Source(builder, "RepairFieldTaskBindings");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, Defect>(builder, "DefectId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, RepairObligation>(builder, "ObligationId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, FieldInspectionTask>(builder, "TaskId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, FieldInspectionAssignment>(builder, "AssignmentId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, ApplicationUser>(builder, "CrewId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, ApplicationUser>(builder, "AssignedBy");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, RepairExecutionAuthorization>(builder, "AuthorizationId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, RepairPolicyRevision>(builder, "PolicyRevisionId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, RoadSectionVersion>(builder, "RouteVersionId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, RoadSegmentSet>(builder, "SegmentSetId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, PavementLayoutRevision>(builder, "LayoutRevisionId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, GeometryMapPublication>(builder, "MapPublicationId");
        RepairProducerMapping.Reference<RepairFieldTaskBinding, CrsProfileRevision>(builder, "CrsProfileRevisionId");
        builder.HasIndex(x => x.TaskId).IsUnique(); builder.HasIndex(x => x.AssignmentId).IsUnique();
        RepairProducerMapping.Text(builder, "PolicyContentHash", 64); RepairProducerMapping.Text(builder, "PlanHash", 64);
        RepairProducerMapping.Text(builder, "ChecklistVersion", 200); RepairProducerMapping.Text(builder, "LocationVersion", 200);
        RepairProducerMapping.Text(builder, "SlabId", 160); RepairProducerMapping.Text(builder, "Reason", 2000);
        RepairProducerMapping.Text(builder, "TaskVersion", 24); RepairProducerMapping.Text(builder, "AssignmentVersion", 64);
    }
}
public sealed class RepairMeasurementAssessmentConfiguration : IEntityTypeConfiguration<RepairMeasurementAssessment>
{
    public void Configure(EntityTypeBuilder<RepairMeasurementAssessment> builder)
    {
        RepairProducerMapping.Source(builder, "RepairMeasurementAssessments");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, RepairFieldTaskBinding>(builder, "BindingId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, FieldInspectionTask>(builder, "TaskId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, FieldInspectionAssignment>(builder, "AssignmentId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, ApplicationUser>(builder, "OriginalActorId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, FieldTaskStartOrigin>(builder, "FirstStartId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, FieldInspectionSession>(builder, "SessionId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, FieldInspectionOperationOrigin>(builder, "OperationOriginId");
        RepairProducerMapping.Reference<RepairMeasurementAssessment, FieldInspectionSubmission>(builder, "FormalSourceSubmissionId");
        builder.HasIndex(x => new { x.ProjectId, x.OriginId }).IsUnique(); builder.HasIndex(x => x.SessionId).IsUnique();
        RepairProducerMapping.Text(builder, "ContentHash", 64); RepairProducerMapping.Text(builder, "Stage", 40);
        RepairProducerMapping.Text(builder, "Readiness", 40); RepairProducerMapping.Text(builder, "LocationState", 40);
        builder.OwnsMany(x => x.Measurements, rows =>
        {
            rows.ToTable("RepairAssessmentMeasurements", table => table.HasTrigger("TR_RepairAssessmentMeasurements_Immutable"));
            rows.WithOwner().HasForeignKey("AssessmentId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            rows.HasKey("AssessmentId", nameof(RepairAssessmentMeasurementReference.MeasurementId));
            rows.HasOne<GroundTruthMeasurement>().WithMany().HasForeignKey(x => x.MeasurementId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.OwnsMany(x => x.Evidence, rows =>
        {
            rows.ToTable("RepairAssessmentEvidence", table => table.HasTrigger("TR_RepairAssessmentEvidence_Immutable"));
            rows.WithOwner().HasForeignKey("AssessmentId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            rows.HasKey(x => x.Id); rows.Property(x => x.Id).ValueGeneratedNever();
            rows.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
            rows.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActualUploaderId).OnDelete(DeleteBehavior.Restrict);
            rows.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OriginalActorId).OnDelete(DeleteBehavior.Restrict);
            rows.HasOne<FieldInspectionEvidenceReuseDecision>().WithMany().HasForeignKey(x => x.ReuseDecisionId).OnDelete(DeleteBehavior.Restrict);
            rows.Property(x => x.Purpose).HasMaxLength(40); rows.Property(x => x.MediaType).HasMaxLength(120);
            rows.Property(x => x.ChecksumSha256).HasMaxLength(64); rows.Property(x => x.FileVersion).HasMaxLength(200);
            rows.Property(x => x.StateAtIntake).HasMaxLength(40); rows.HasIndex("AssessmentId", "CaptureOriginId").IsUnique();
        });
        builder.Navigation(x => x.Measurements).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Evidence).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class RepairExecutionStartConfiguration : IEntityTypeConfiguration<RepairExecutionStart>
{
    public void Configure(EntityTypeBuilder<RepairExecutionStart> builder)
    {
        RepairProducerMapping.Source(builder, "RepairExecutionStarts");
        RepairProducerMapping.Reference<RepairExecutionStart, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairExecutionStart, RepairFieldTaskBinding>(builder, "BindingId");
        RepairProducerMapping.Reference<RepairExecutionStart, RepairMeasurementAssessment>(builder, "AssessmentId");
        RepairProducerMapping.Reference<RepairExecutionStart, FieldTaskStartOrigin>(builder, "FirstStartId");
        RepairProducerMapping.Reference<RepairExecutionStart, ApplicationUser>(builder, "OriginalActorId");
        RepairProducerMapping.Reference<RepairExecutionStart, FieldInspectionOperationOrigin>(builder, "OperationOriginId");
        RepairProducerMapping.Reference<RepairExecutionStart, RepairEligibilityAssessment>(builder, "EligibilityAssessmentId");
        builder.HasIndex(x => new { x.ProjectId, x.OriginId }).IsUnique();
        RepairProducerMapping.Text(builder, "ContentHash", 64); RepairProducerMapping.Text(builder, "AssessmentContentHash", 64);
        RepairProducerMapping.Text(builder, "ChecklistVersion", 200);
    }
}
public sealed class RepairExecutionFinishConfiguration : IEntityTypeConfiguration<RepairExecutionFinish>
{
    public void Configure(EntityTypeBuilder<RepairExecutionFinish> builder)
    {
        RepairProducerMapping.Source(builder, "RepairExecutionFinishes");
        RepairProducerMapping.Reference<RepairExecutionFinish, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairExecutionFinish, RepairFieldTaskBinding>(builder, "BindingId");
        RepairProducerMapping.Reference<RepairExecutionFinish, RepairExecutionStart>(builder, "ExecutionStartId");
        RepairProducerMapping.Reference<RepairExecutionFinish, ApplicationUser>(builder, "OriginalActorId");
        RepairProducerMapping.Reference<RepairExecutionFinish, FieldInspectionOperationOrigin>(builder, "OperationOriginId");
        builder.HasIndex(x => x.ExecutionStartId).IsUnique(); builder.HasIndex(x => new { x.ProjectId, x.OriginId }).IsUnique();
        RepairProducerMapping.Text(builder, "ContentHash", 64);
    }
}
public sealed class RepairAttemptSubmissionLinkConfiguration : IEntityTypeConfiguration<RepairAttemptSubmissionLink>
{
    public void Configure(EntityTypeBuilder<RepairAttemptSubmissionLink> builder)
    {
        RepairProducerMapping.Source(builder, "RepairAttemptSubmissionLinks");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, RepairFieldTaskBinding>(builder, "BindingId");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, RepairAttempt>(builder, "AttemptId");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, FieldInspectionSubmission>(builder, "SubmissionId");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, FieldInspectionSubmission>(builder, "FormalRootSubmissionId");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, RepairAttemptSubmissionLink>(builder, "PreviousLinkId");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, RepairExecutionFinish>(builder, "ExecutionFinishId");
        RepairProducerMapping.Reference<RepairAttemptSubmissionLink, DeadlineClock>(builder, "ReviewClockId");
        builder.HasIndex(x => x.SubmissionId).IsUnique();
        builder.HasIndex(x => x.PreviousLinkId).IsUnique().HasFilter("[PreviousLinkId] IS NOT NULL");
        RepairProducerMapping.Text(builder, "SubmissionContentHash", 64);
    }
}
public sealed class RepairAttemptReviewConfiguration : IEntityTypeConfiguration<RepairAttemptReview>
{
    public void Configure(EntityTypeBuilder<RepairAttemptReview> builder)
    {
        RepairProducerMapping.Source(builder, "RepairAttemptReviews");
        RepairProducerMapping.Reference<RepairAttemptReview, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairAttemptReview, RepairFieldTaskBinding>(builder, "BindingId");
        RepairProducerMapping.Reference<RepairAttemptReview, RepairAttempt>(builder, "AttemptId");
        RepairProducerMapping.Reference<RepairAttemptReview, RepairAttemptSubmissionLink>(builder, "IntakeLinkId");
        RepairProducerMapping.Reference<RepairAttemptReview, FieldInspectionSubmission>(builder, "SubmissionId");
        RepairProducerMapping.Reference<RepairAttemptReview, ApplicationUser>(builder, "ActorId");
        RepairProducerMapping.Text(builder, "SubmissionContentHash", 64); RepairProducerMapping.Text(builder, "PlanHash", 64);
        RepairProducerMapping.Text(builder, "ChecklistVersion", 200); RepairProducerMapping.Text(builder, "Decision", 40);
        RepairProducerMapping.Text(builder, "Reason", 2000);
    }
}
public sealed class RepairItemLifecycleEventConfiguration : IEntityTypeConfiguration<RepairItemLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<RepairItemLifecycleEvent> builder)
    {
        RepairProducerMapping.Source(builder, "RepairItemLifecycleEvents");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, Defect>(builder, "DefectId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, RepairObligation>(builder, "ObligationId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, ApplicationUser>(builder, "ActorId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, RepairFieldTaskBinding>(builder, "BindingId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, RepairAttempt>(builder, "AttemptId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, FieldInspectionSubmission>(builder, "SubmissionId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, RepairAttemptReview>(builder, "ReviewId");
        RepairProducerMapping.Reference<RepairItemLifecycleEvent, RepairDecision>(builder, "DecisionId");
        RepairProducerMapping.Text(builder, "Kind", 40); RepairProducerMapping.Text(builder, "Reason", 2000);
        RepairProducerMapping.Text(builder, "SourceVersion", 200);
    }
}
public sealed class RepairNormalSuccessorConfiguration : IEntityTypeConfiguration<RepairNormalSuccessor>
{
    public void Configure(EntityTypeBuilder<RepairNormalSuccessor> builder)
    {
        RepairProducerMapping.Source(builder, "RepairNormalSuccessors");
        RepairProducerMapping.Reference<RepairNormalSuccessor, RepairObligation>(builder, "ObligationId");
        RepairProducerMapping.Reference<RepairNormalSuccessor, RepairItem>(builder, "SourceItemId");
        RepairProducerMapping.Reference<RepairNormalSuccessor, RepairItem>(builder, "TargetItemId");
        RepairProducerMapping.Reference<RepairNormalSuccessor, RepairDecision>(builder, "SourceDecisionId");
        RepairProducerMapping.Reference<RepairNormalSuccessor, RepairMeasurementAssessment>(builder, "SourceAssessmentId");
        RepairProducerMapping.Reference<RepairNormalSuccessor, RepairItemLifecycleEvent>(builder, "SourceCancellationEventId");
        RepairProducerMapping.Reference<RepairNormalSuccessor, FieldInspectionTaskEvent>(builder, "SourceHandoverEventId");
        RepairProducerMapping.Reference<RepairNormalSuccessor, ApplicationUser>(builder, "ActorId");
        builder.HasIndex(x => x.SourceItemId).IsUnique(); builder.HasIndex(x => x.TargetItemId).IsUnique();
        RepairProducerMapping.Text(builder, "Reason", 2000);
    }
}
public sealed class RepairEligibilityAssessmentConfiguration : IEntityTypeConfiguration<RepairEligibilityAssessment>
{
    public void Configure(EntityTypeBuilder<RepairEligibilityAssessment> builder)
    {
        RepairProducerMapping.Source(builder, "RepairEligibilityAssessments");
        RepairProducerMapping.Reference<RepairEligibilityAssessment, RepairItem>(builder, "ItemId");
        RepairProducerMapping.Reference<RepairEligibilityAssessment, RepairFieldTaskBinding>(builder, "BindingId");
        RepairProducerMapping.Reference<RepairEligibilityAssessment, RepairMeasurementAssessment>(builder, "AssessmentId");
        RepairProducerMapping.Reference<RepairEligibilityAssessment, RoadSection>(builder, "RoadSectionId");
        RepairProducerMapping.Reference<RepairEligibilityAssessment, RepairPolicyRevision>(builder, "PolicyRevisionId");
        RepairProducerMapping.Text(builder, "PolicyContentHash", 64); RepairProducerMapping.Text(builder, "SourceFactsHash", 64);
        RepairProducerMapping.Text(builder, "SourceMapping", 200);
        builder.OwnsMany(x => x.Warranties, rows =>
        {
            rows.ToTable("RepairEligibilityWarrantySources", table => table.HasTrigger("TR_RepairEligibilityWarrantySources_Immutable"));
            rows.WithOwner().HasForeignKey("EligibilityAssessmentId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            rows.HasKey("EligibilityAssessmentId", nameof(RepairEligibilityWarrantySource.WarrantyId));
            rows.HasOne<Warranty>().WithMany().HasForeignKey(x => x.WarrantyId).OnDelete(DeleteBehavior.Restrict);
            rows.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Restrict);
            rows.Property(x => x.RowVersion).HasMaxLength(24); rows.Property(x => x.ContentHash).HasMaxLength(64);
        });
        builder.OwnsMany(x => x.Handovers, rows =>
        {
            rows.ToTable("RepairEligibilityHandoverSources", table => table.HasTrigger("TR_RepairEligibilityHandoverSources_Immutable"));
            rows.WithOwner().HasForeignKey("EligibilityAssessmentId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            rows.HasKey("EligibilityAssessmentId", nameof(RepairEligibilityHandoverSource.HandoverDocumentId));
            rows.HasOne<HandoverDocument>().WithMany().HasForeignKey(x => x.HandoverDocumentId).OnDelete(DeleteBehavior.Restrict);
            rows.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
            rows.Property(x => x.RowVersion).HasMaxLength(24); rows.Property(x => x.ContentHash).HasMaxLength(64);
        });
        builder.Navigation(x => x.Warranties).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Handovers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
