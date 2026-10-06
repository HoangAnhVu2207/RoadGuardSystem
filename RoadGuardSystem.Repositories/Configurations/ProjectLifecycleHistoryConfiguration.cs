using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Defects;

namespace RoadGuardSystem.Repositories.Configurations;

internal static class ProjectLifecycleHistoryConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var builder=model.Entity<ProjectLifecycleHistoryRecord>();
        builder.ToTable("ProjectLifecycleHistory",table=>
        {
            table.HasTrigger("TR_ProjectLifecycleHistory_Immutable");
            table.HasTrigger("TR_ProjectLifecycleHistory_Scope");
            table.HasCheckConstraint("CK_ProjectLifecycleHistory_Source","[Kind] IN (1,2,3,4,5,6,7,8) AND [SourceDisposition] IN ('CANDIDATE','TARGET_CONFIRMED') AND ([SourceDisposition]='CANDIDATE' OR LEN([AuthoritySourceReference])>0)");
            table.HasCheckConstraint("CK_ProjectLifecycleHistory_Facts","ISJSON([FactsJson])=1");
        });
        builder.HasKey(row=>row.Id);builder.Property(row=>row.Id).ValueGeneratedNever();
        builder.HasAlternateKey(row=>new{row.Id,row.ProjectId});
        builder.Property(row=>row.Kind).HasConversion<byte>();
        builder.Property(row=>row.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(row=>row.BasisReference).HasMaxLength(2000).IsRequired();
        builder.Property(row=>row.AuthoritySourceReference).HasMaxLength(2000).IsRequired();
        builder.Property(row=>row.SourceDisposition).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(row=>row.FactsJson).IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(row=>row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(row=>row.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(row=>row.ReceiverId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairObligation>().WithMany().HasForeignKey(row=>row.ObligationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(row=>row.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(row=>row.LinkedDefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProjectLifecycleHistoryRecord>().WithMany().HasForeignKey(row=>new{row.OperationalClosureId,row.ProjectId})
            .HasPrincipalKey(row=>new{row.Id,row.ProjectId}).OnDelete(DeleteBehavior.Restrict);
    }
}
