using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class CaseRoutingEventConfiguration : IEntityTypeConfiguration<CaseRoutingEvent>
{
    public void Configure(EntityTypeBuilder<CaseRoutingEvent> builder)
    {
        builder.ToTable("CaseRoutingEvents", table =>
        {
            table.HasTrigger("TR_CaseRoutingEvents_Immutable");
            table.HasCheckConstraint("CK_CaseRoutingEvents_Kind", "([Kind] COLLATE Latin1_General_100_BIN2='DECISION' AND [Outcome] IS NOT NULL AND [Outcome] COLLATE Latin1_General_100_BIN2 IN ('NEEDS_LOCATION_EVIDENCE','OUTSIDE_COMPANY_SCOPE')) OR ([Kind] COLLATE Latin1_General_100_BIN2='TRIAGE' AND [Outcome] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
        builder.Property(x => x.Kind).HasMaxLength(20);
        builder.Property(x => x.Outcome).HasMaxLength(40);
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.CaseSourceVersion).HasMaxLength(64);
        builder.Property(x => x.EvaluationVersion).HasMaxLength(64);
        builder.Property(x => x.RegistryVersion).HasMaxLength(64);
        builder.Property(x => x.TemporalBasis).HasMaxLength(40);
        builder.HasOne<IncidentCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CaseRoutingEvent>().WithMany().HasForeignKey(x => x.SupersedesId).OnDelete(DeleteBehavior.Restrict);
    }
}
