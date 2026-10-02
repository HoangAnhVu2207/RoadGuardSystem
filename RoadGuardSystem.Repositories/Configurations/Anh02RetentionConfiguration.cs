using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
namespace RoadGuardSystem.Repositories.Configurations;

public sealed class Anh02RetentionConfiguration : IEntityTypeConfiguration<RetentionBasisHead>, IEntityTypeConfiguration<RetentionBasisRevision>,
    IEntityTypeConfiguration<RetentionHold>, IEntityTypeConfiguration<RetentionHoldHistory>, IEntityTypeConfiguration<RetentionEvaluation>, IEntityTypeConfiguration<RetentionEvaluationItem>
{
    public void Configure(EntityTypeBuilder<RetentionBasisHead> builder)
    {
        var b = builder;
        b.ToTable("RetentionBasisHeads"); b.HasKey(x => x.FileId); b.Property(x => x.FileId).ValueGeneratedNever(); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RetentionBasisRevision>().WithMany().HasForeignKey(x => new { x.RevisionId, x.FileId }).HasPrincipalKey(x => new { x.Id, x.FileId }).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<RetentionBasisRevision> builder)
    {
        var b = builder;
        b.ToTable("RetentionBasisRevisions", t => { t.HasTrigger("TR_RetentionBasisRevisions_Immutable"); t.HasCheckConstraint("CK_RetentionBasis_Revision", "[Revision]>0 AND [PolicyVersion]='pr41a.v1' AND [Classification] IN ('EVIDENCE','TEMPORARY_EXPORT') AND ISJSON([WarrantyReferencesJson])=1"); });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.PolicyVersion).HasMaxLength(40); b.Property(x => x.Classification).HasMaxLength(40);
        b.Property(x => x.InventoryVersion).HasMaxLength(64); b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasIndex(x => new { x.FileId, x.Revision }).IsUnique();
        b.HasAlternateKey(x => new { x.Id, x.FileId });
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ConfirmedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RetentionBasisRevision>().WithMany().HasForeignKey(x => new { x.SupersedesId, x.FileId }).HasPrincipalKey(x => new { x.Id, x.FileId }).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<RetentionHold> builder)
    {
        var b = builder;
        b.ToTable("RetentionHolds", t => t.HasCheckConstraint("CK_RetentionHold_State", "[ScopeType] IN ('PROJECT','FILE') AND [State] IN ('ACTIVE','RELEASED') AND (([State]='ACTIVE' AND [ReleasedBy] IS NULL AND [ReleasedAt] IS NULL) OR ([State]='RELEASED' AND [ReleasedBy] IS NOT NULL AND [ReleasedAt] IS NOT NULL))"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.ScopeType).HasMaxLength(20); b.Property(x => x.State).HasMaxLength(20); b.Property(x => x.Reason).HasMaxLength(2000); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.ScopeType, x.ScopeId, x.State });
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReleasedBy).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<RetentionHoldHistory> builder)
    {
        var b = builder;
        b.ToTable("RetentionHoldHistories", t => { t.HasTrigger("TR_RetentionHoldHistories_Immutable"); t.HasCheckConstraint("CK_RetentionHoldHistory_State", "[State] IN ('ACTIVE','RELEASED')"); });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.State).HasMaxLength(20); b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne<RetentionHold>().WithMany().HasForeignKey(x => x.HoldId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.HoldId, x.OccurredAt });
    }
    public void Configure(EntityTypeBuilder<RetentionEvaluation> builder)
    {
        var b = builder;
        b.ToTable("RetentionEvaluations", t => t.HasCheckConstraint("CK_RetentionEvaluation_State", "[Status] IN ('QUEUED','COMPLETE') AND (([Status]='QUEUED' AND [EvaluatedAt] IS NULL) OR ([Status]='COMPLETE' AND [EvaluatedAt] IS NOT NULL))"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.Status).HasMaxLength(20); b.Property(x => x.PolicyVersion).HasMaxLength(40); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
    }
    public void Configure(EntityTypeBuilder<RetentionEvaluationItem> builder)
    {
        var b = builder;
        b.ToTable("RetentionEvaluationItems", t => { t.HasTrigger("TR_RetentionEvaluationItems_Immutable"); t.HasCheckConstraint("CK_RetentionEvaluationItem_State", "[Eligibility] IN ('BLOCKED_HOLD','WAITING_RETENTION_BASIS','RETAIN_UNTIL','ELIGIBLE_FOR_REVIEW')"); });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.Eligibility).HasMaxLength(40); b.Property(x => x.BasisVersion).HasMaxLength(64); b.Property(x => x.InventoryVersion).HasMaxLength(64); b.Property(x => x.HoldVersion).HasMaxLength(64);
        b.HasIndex(x => new { x.EvaluationId, x.FileId }).IsUnique();
        b.HasOne<RetentionEvaluation>().WithMany().HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
