using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Files;

namespace RoadGuardSystem.Repositories.Configurations;

internal static class LD06LifecycleConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var action = model.Entity<LD06LifecycleAction>();
        action.ToTable("LD06LifecycleActions", t =>
        {
            t.HasTrigger("TR_LD06LifecycleActions_Immutable"); t.HasTrigger("TR_LD06LifecycleActions_Scope");
            t.HasCheckConstraint("CK_LD06LifecycleActions_Kind", "[Kind] BETWEEN 1 AND 7 AND ISJSON([FactsJson])=1");
        });
        action.HasKey(x => x.Id); action.Property(x => x.Id).ValueGeneratedNever();
        action.Property(x => x.Kind).HasConversion<byte>();
        action.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        action.Property(x => x.ScopeHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        action.Property(x => x.FactsJson).IsRequired();
        action.HasIndex(x => new { x.SourceActionId, x.Kind }).IsUnique().HasFilter("[SourceActionId] IS NOT NULL AND [Kind] IN (2,7)");
        action.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        action.HasOne<Project>().WithMany().HasForeignKey(x => x.ReceivingProjectId).OnDelete(DeleteBehavior.Restrict);
        action.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        action.HasOne<LD06LifecycleAction>().WithMany().HasForeignKey(x => x.SourceActionId).OnDelete(DeleteBehavior.Restrict);
        action.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        action.HasOne<Defect>().WithMany().HasForeignKey(x => x.LinkedDefectId).OnDelete(DeleteBehavior.Restrict);
        action.HasOne<RepairObligation>().WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        action.HasOne<RepairDecision>().WithMany().HasForeignKey(x => x.PriorRepairDecisionId).OnDelete(DeleteBehavior.Restrict);
        var owner = model.Entity<ObligationResponsibility>();
        owner.ToTable("ObligationResponsibilities", t => t.HasTrigger("TR_ObligationResponsibilities_Scope"));
        owner.HasKey(x => x.ObligationId); owner.Property(x => x.ObligationId).ValueGeneratedNever();
        owner.Property(x => x.RowVersion).IsRowVersion();
        owner.HasOne<RepairObligation>().WithMany().HasForeignKey(x => x.ObligationId).OnDelete(DeleteBehavior.Restrict);
        owner.HasOne<Project>().WithMany().HasForeignKey(x => x.OriginProjectId).OnDelete(DeleteBehavior.Restrict);
        owner.HasOne<Project>().WithMany().HasForeignKey(x => x.CurrentProjectId).OnDelete(DeleteBehavior.Restrict);
        owner.HasOne<LD06LifecycleAction>().WithMany().HasForeignKey(x => x.AcceptanceActionId).OnDelete(DeleteBehavior.Restrict);
        var evidence = model.Entity<LD06ActionEvidence>();
        evidence.ToTable("LD06ActionEvidence", t => t.HasTrigger("TR_LD06ActionEvidence_Immutable"));
        evidence.HasKey(x => new { x.ActionId, x.FileId });
        evidence.Property(x => x.Checksum).HasMaxLength(64).IsUnicode(false).IsRequired();
        evidence.HasOne<LD06LifecycleAction>().WithMany().HasForeignKey(x => x.ActionId).OnDelete(DeleteBehavior.Restrict);
        evidence.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
