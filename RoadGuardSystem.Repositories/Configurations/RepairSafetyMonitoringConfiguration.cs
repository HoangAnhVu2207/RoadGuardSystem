using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SafetyResponsibilityTransferConfiguration : IEntityTypeConfiguration<SafetyResponsibilityTransfer>
{
    public void Configure(EntityTypeBuilder<SafetyResponsibilityTransfer> builder)
    {
        builder.ToTable("RepairSafetyResponsibilityTransfers", table => table.HasTrigger("TR_RepairSafetyResponsibilityTransfers_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.MeasureId, x.Id });
        builder.HasOne<TemporarySafetyMeasure>().WithMany(x => x.Transfers).HasForeignKey(x => x.MeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PreviousActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.NextActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ChangedBy).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Handover).HasMaxLength(2000); builder.Property(x => x.Reason).HasMaxLength(2000);
    }
}
public sealed class RepairSafetyMonitoringConfiguration : IEntityTypeConfiguration<RepairSafetyMonitoring>
{
    public void Configure(EntityTypeBuilder<RepairSafetyMonitoring> builder)
    {
        builder.ToTable("RepairSafetyMonitoring", table => table.HasTrigger("TR_RepairSafetyMonitoring_Scope"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasOne(x => x.Measure).WithMany().HasForeignKey(x => x.MeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MeasureId).IsUnique();
        builder.HasIndex(x => x.SafetyObligationId).IsUnique();
        builder.HasOne(x => x.SafetyObligation).WithMany().HasForeignKey(x => x.SafetyObligationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FormalObligation).WithMany().HasForeignKey(x => x.FormalObligationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairSafetyCheck>().WithMany().HasForeignKey(x => x.CurrentCheckId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Checks).WithOne().HasForeignKey(x => x.MeasureId).HasPrincipalKey(x => x.MeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Warnings).WithOne().HasForeignKey(x => x.MonitoringId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Acknowledgements).WithOne().HasForeignKey(x => x.MonitoringId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Checks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Warnings).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Acknowledgements).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class RepairSafetyCheckConfiguration : IEntityTypeConfiguration<RepairSafetyCheck>
{
    public void Configure(EntityTypeBuilder<RepairSafetyCheck> builder)
    {
        builder.ToTable("RepairSafetyChecks", table => table.HasTrigger("TR_RepairSafetyChecks_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<TemporarySafetyMeasure>().WithMany().HasForeignKey(x => x.MeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Findings).HasMaxLength(2000);
        builder.Property(x => x.EvidenceIds).HasConversion(value => RepairHistoryJson.Encode(value), json => RepairHistoryJson.Decode<Guid>(json))
            .Metadata.SetValueComparer(RepairHistoryJson.Comparer<Guid>());
        builder.OwnsMany(x => x.Evidence, rows =>
        {
            rows.ToTable("RepairSafetyCheckEvidence", table => table.HasTrigger("TR_RepairSafetyCheckEvidence_Immutable"));
            rows.WithOwner().HasForeignKey("CheckId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            rows.HasKey("CheckId", nameof(RepairSafetyEvidenceReference.FileId));
            rows.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
            rows.Property(x => x.FileVersion).HasMaxLength(200); rows.Property(x => x.Hash).HasMaxLength(64);
            rows.Property(x => x.SourceKind).HasMaxLength(60);
            rows.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActualUploaderId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(x => x.Evidence).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class RepairDangerWarningConfiguration : IEntityTypeConfiguration<RepairDangerWarning>
{
    public void Configure(EntityTypeBuilder<RepairDangerWarning> builder)
    {
        builder.ToTable("RepairDangerWarnings", table => table.HasTrigger("TR_RepairDangerWarnings_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResponsibleActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.MonitoringId, x.SourceId }).IsUnique(); builder.Property(x => x.Reason).HasMaxLength(2000);
    }
}
public sealed class RepairDangerAcknowledgementConfiguration : IEntityTypeConfiguration<RepairDangerAcknowledgement>
{
    public void Configure(EntityTypeBuilder<RepairDangerAcknowledgement> builder)
    {
        builder.ToTable("RepairDangerAcknowledgements", table => table.HasTrigger("TR_RepairDangerAcknowledgements_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<RepairDangerWarning>().WithMany().HasForeignKey(x => x.WarningId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.WarningId).IsUnique(); builder.Property(x => x.Reason).HasMaxLength(2000);
    }
}
