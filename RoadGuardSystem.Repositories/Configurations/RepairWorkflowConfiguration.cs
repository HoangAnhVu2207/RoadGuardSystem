using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RepairPackageConfiguration : IEntityTypeConfiguration<RepairPackage>
{
    public void Configure(EntityTypeBuilder<RepairPackage> builder)
    {
        builder.ToTable("RepairPackages", table => table.HasTrigger("TR_RepairPackages_Scope")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property<byte[]>("RowVersion").IsRowVersion(); builder.Property<long>("MutationRevision").HasDefaultValue(0L); builder.Ignore(x => x.IsComplete);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Obligations).WithOne().HasForeignKey("PackageId").OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey("PackageId").OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class RepairObligationConfiguration : IEntityTypeConfiguration<RepairObligation>
{
    public void Configure(EntityTypeBuilder<RepairObligation> builder)
    {
        builder.ToTable("RepairObligations", table => table.HasTrigger("TR_RepairObligations_Scope")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property<byte[]>("RowVersion").IsRowVersion(); builder.Ignore(x => x.IsResolved);
        builder.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.EffectiveResolutionHeadDecisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.EffectiveResolutionDecisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairItem>().WithMany().HasForeignKey(x => x.CurrentRepairItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldTaskStartOrigin>().WithMany().HasForeignKey(x => x.OriginalCrewFirstStartId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(x => x.Scope, scope =>
        {
            scope.ToTable("RepairActualScopes", table => table.HasTrigger("TR_RepairActualScopes_Scope")); scope.WithOwner().HasForeignKey("ObligationId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            scope.Property(x => x.Id).ValueGeneratedNever(); scope.HasKey(x => x.Id);
            scope.Property(x => x.LocationVersion).HasMaxLength(200); scope.Property(x => x.RouteLabel).HasMaxLength(200);
            scope.Property(x => x.From).HasPrecision(18, 3); scope.Property(x => x.To).HasPrecision(18, 3);
            scope.Property(x => x.OffsetFrom).HasPrecision(18, 3); scope.Property(x => x.OffsetTo).HasPrecision(18, 3);
            scope.HasOne<RoadSection>().WithMany().HasForeignKey(x => x.PhysicalRoadId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.OwnsMany(x => x.ResolutionHistory, history =>
        {
            history.ToTable("RepairObligationResolutionEvents", table => table.HasTrigger("TR_RepairObligationResolutionEvents_Immutable"));
            history.WithOwner().HasForeignKey("ObligationId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            history.HasKey(x => x.DecisionId); history.Property(x => x.DecisionId).ValueGeneratedNever();
            history.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.DecisionId).OnDelete(DeleteBehavior.Restrict);
            history.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.SupersedesDecisionId).OnDelete(DeleteBehavior.Restrict);
            history.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.PreviousHeadDecisionId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(x => x.ResolutionHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class RepairItemConfiguration : IEntityTypeConfiguration<RepairItem>
{
    public void Configure(EntityTypeBuilder<RepairItem> builder)
    {
        builder.ToTable("RepairItems", table => table.HasTrigger("TR_RepairItems_Scope")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property<byte[]>("RowVersion").IsRowVersion(); builder.Ignore(x => x.EffectiveDecision); builder.Ignore(x => x.IsEffectivelyConfirmed); builder.Ignore(x => x.Presentation);
        builder.Property(x => x.RepairPlan).HasMaxLength(2000); builder.Property(x => x.ChecklistVersion).HasMaxLength(200);
        builder.Property(x => x.ProposalPlanHash).HasMaxLength(64); builder.Property(x => x.ApprovedPlanHash).HasMaxLength(64);
        builder.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.EffectiveDecisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairFieldTaskBinding>().WithMany().HasForeignKey(x => x.CurrentBindingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairMeasurementAssessment>().WithMany().HasForeignKey(x => x.CurrentAssessmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairExecutionStart>().WithMany().HasForeignKey(x => x.CurrentExecutionStartId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairExecutionFinish>().WithMany().HasForeignKey(x => x.CurrentExecutionFinishId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairAttempt>().WithMany().HasForeignKey(x => x.CurrentAttemptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairAttemptSubmissionLink>().WithMany().HasForeignKey(x => x.CurrentIntakeLinkId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionSubmission>().WithMany().HasForeignKey(x => x.EffectiveIntakeSubmissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairAttemptReview>().WithMany().HasForeignKey(x => x.CurrentReviewId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairItem>().WithMany().HasForeignKey(x => x.PredecessorItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairItem>().WithMany().HasForeignKey(x => x.SupersededByItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairObligation>().WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CrewId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.CancellationReason).HasMaxLength(2000);
        builder.OwnsOne(x => x.Handover, handover =>
        {
            handover.ToTable("RepairWorkHandovers", table => table.HasTrigger("TR_RepairWorkHandovers_Immutable"));
            handover.WithOwner().HasForeignKey("ItemId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            handover.HasKey(x => x.Id); handover.Property(x => x.Id).ValueGeneratedNever();
            handover.Property(x => x.PerformedScope).HasMaxLength(2000); handover.Property(x => x.SafetyState).HasMaxLength(2000);
            handover.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.FromActorId).OnDelete(DeleteBehavior.Restrict);
            handover.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ToActorId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.HasMany(x => x.Attempts).WithOne().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Decisions).WithOne().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.ReviewRequests).WithOne().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Attempts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Decisions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ReviewRequests).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class RepairAttemptConfiguration : IEntityTypeConfiguration<RepairAttempt>
{
    public void Configure(EntityTypeBuilder<RepairAttempt> builder)
    {
        builder.ToTable("RepairAttempts", table => table.HasTrigger("TR_RepairAttempts_Immutable")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<RepairObligation>().WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CrewId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProjectId, x.OriginId }).IsUnique(); builder.Property(x => x.PayloadHash).HasMaxLength(64);
        builder.Property(x => x.LocationVersion).HasMaxLength(200); builder.Property(x => x.UnperformedReason).HasMaxLength(2000);
        builder.OwnsMany(x => x.Evidence, evidence =>
        {
            evidence.ToTable("RepairAttemptEvidence", table => table.HasTrigger("TR_RepairAttemptEvidence_Immutable"));
            evidence.WithOwner().HasForeignKey("AttemptId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            evidence.HasKey("AttemptId", nameof(RepairEvidenceReference.FileId), nameof(RepairEvidenceReference.Purpose));
            evidence.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
            evidence.Property(x => x.FileVersion).HasMaxLength(200); evidence.Property(x => x.Hash).HasMaxLength(64); evidence.Property(x => x.SourceKind).HasMaxLength(60);
        });
    }
}
public sealed class RepairDecisionConfiguration : IEntityTypeConfiguration<RepairDecision>
{
    public void Configure(EntityTypeBuilder<RepairDecision> builder)
    {
        builder.ToTable("RepairDecisions", table => table.HasTrigger("TR_RepairDecisions_Immutable")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Ignore(x => x.Accepted);
        builder.HasOne<RepairObligation>().WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.SupersedesDecisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.PreviousObligationHeadDecisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.SupersedesDecisionId).IsUnique().HasFilter("[SupersedesDecisionId] IS NOT NULL"); builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.OwnsOne(x => x.Basis, basis =>
        {
            basis.Property(x => x.Text).HasMaxLength(2000);
            basis.OwnsMany(x => x.Evidence, evidence =>
            {
                evidence.ToTable("RepairCorrectionEvidence", table => table.HasTrigger("TR_RepairCorrectionEvidence_Immutable"));
                evidence.WithOwner().HasForeignKey("DecisionId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
                evidence.HasKey("DecisionId", nameof(RepairEvidenceReference.FileId));
                evidence.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
                evidence.Property(x => x.FileVersion).HasMaxLength(200); evidence.Property(x => x.Hash).HasMaxLength(64); evidence.Property(x => x.SourceKind).HasMaxLength(60);
            });
        });
    }
}
public sealed class RepairReviewRequestConfiguration : IEntityTypeConfiguration<RepairReviewRequest>
{
    public void Configure(EntityTypeBuilder<RepairReviewRequest> builder)
    {
        builder.ToTable("RepairReviewRequests", table => table.HasTrigger("TR_RepairReviewRequests_Immutable")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.DecisionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Reason).HasMaxLength(2000);
    }
}
