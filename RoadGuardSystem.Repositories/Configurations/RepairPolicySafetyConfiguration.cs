using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Configurations;

internal static class RepairHistoryJson
{
    internal static string Encode<T>(IReadOnlyList<T> values) => JsonSerializer.Serialize(values);
    internal static IReadOnlyList<T> Decode<T>(string json) => Array.AsReadOnly(JsonSerializer.Deserialize<T[]>(json) ?? []);
    internal static ValueComparer<IReadOnlyList<T>> Comparer<T>() => new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)), value => value.ToArray());
}
public sealed class RepairPolicyDraftConfiguration : IEntityTypeConfiguration<RepairPolicyDraft>
{
    public void Configure(EntityTypeBuilder<RepairPolicyDraft> builder)
    {
        builder.ToTable("RepairPolicyDrafts", table => table.HasTrigger("TR_RepairPolicyDrafts_Scope")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Changes).WithOne().HasForeignKey("DraftId").OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Changes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<RepairPolicyDraftChange>().WithMany().HasForeignKey(x => x.CurrentChangeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PublishedRevision).WithMany().HasForeignKey(x => x.PublishedRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class RepairPolicyDraftChangeConfiguration : IEntityTypeConfiguration<RepairPolicyDraftChange>
{
    public void Configure(EntityTypeBuilder<RepairPolicyDraftChange> builder)
    {
        builder.ToTable("RepairPolicyDraftChanges", table => table.HasTrigger("TR_RepairPolicyDraftChanges_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Reason).HasMaxLength(2000); builder.Property(x => x.DefectTypeCode).HasMaxLength(200);
        builder.Property(x => x.ChecklistVersion).HasMaxLength(200);
        builder.Property(x => x.Measurements).HasConversion(value => RepairHistoryJson.Encode(value), json => RepairHistoryJson.Decode<RepairMeasurementRule>(json))
            .Metadata.SetValueComparer(RepairHistoryJson.Comparer<RepairMeasurementRule>());
        builder.Property(x => x.StopConditions).HasConversion(value => RepairHistoryJson.Encode(value), json => RepairHistoryJson.Decode<string>(json))
            .Metadata.SetValueComparer(RepairHistoryJson.Comparer<string>());
    }
}
public sealed class RepairPolicyRevisionConfiguration : IEntityTypeConfiguration<RepairPolicyRevision>
{
    public void Configure(EntityTypeBuilder<RepairPolicyRevision> builder)
    {
        builder.ToTable("RepairPolicyRevisions", table => table.HasTrigger("TR_RepairPolicyRevisions_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Ignore(x => x.IsRevoked);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PublishedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProjectId, x.Revision }).IsUnique();
        builder.Property(x => x.DefectTypeCode).HasMaxLength(200); builder.Property(x => x.ChecklistVersion).HasMaxLength(200);
        builder.Property(x => x.StopConditions).HasConversion(value => RepairHistoryJson.Encode(value), json => RepairHistoryJson.Decode<string>(json))
            .Metadata.SetValueComparer(RepairHistoryJson.Comparer<string>());
        builder.OwnsMany(x => x.Measurements, rules =>
        {
            rules.ToTable("RepairPolicyMeasurementRules", table => table.HasTrigger("TR_RepairPolicyMeasurementRules_Immutable"));
            rules.WithOwner().HasForeignKey("PolicyRevisionId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            rules.HasKey("PolicyRevisionId", nameof(RepairMeasurementRule.Code)); rules.Property(x => x.Code).HasMaxLength(200);
            rules.Property(x => x.Unit).HasMaxLength(80); rules.Property(x => x.Minimum).HasPrecision(20, 6); rules.Property(x => x.Maximum).HasPrecision(20, 6);
        });
        builder.Navigation(x => x.Measurements).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsMany(x => x.Revocations, revocations =>
        {
            revocations.ToTable("RepairPolicyRevocations", table => table.HasTrigger("TR_RepairPolicyRevocations_Immutable"));
            revocations.WithOwner().HasForeignKey("PolicyRevisionId").Metadata.DeleteBehavior = DeleteBehavior.Restrict;
            revocations.HasKey(x => x.Id); revocations.Property(x => x.Id).ValueGeneratedNever();
            revocations.Property(x => x.Reason).HasMaxLength(2000);
            revocations.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(x => x.Revocations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
public sealed class RepairExecutionAuthorizationConfiguration : IEntityTypeConfiguration<RepairExecutionAuthorization>
{
    public void Configure(EntityTypeBuilder<RepairExecutionAuthorization> builder)
    {
        builder.ToTable("RepairExecutionAuthorizations", table => table.HasTrigger("TR_RepairExecutionAuthorizations_Scope")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property<byte[]>("RowVersion").IsRowVersion(); builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.LocationVersion).HasMaxLength(200); builder.Property(x => x.FirstStartPayloadHash).HasMaxLength(64);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionAssignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CrewId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.IssuedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairPolicyRevision>().WithMany().HasForeignKey(x => x.PolicyRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TaskId).IsUnique();
    }
}
public sealed class TemporarySafetyMeasureConfiguration : IEntityTypeConfiguration<TemporarySafetyMeasure>
{
    public void Configure(EntityTypeBuilder<TemporarySafetyMeasure> builder)
    {
        builder.ToTable("RepairTemporarySafetyMeasures", table => table.HasTrigger("TR_RepairTemporarySafetyMeasures_Scope")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Ignore(x => x.RequiresMonitoring); builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairObligation>().WithMany().HasForeignKey(x => x.FormalRepairObligationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResponsibleActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.InstalledBy).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.CheckSchedule).HasMaxLength(2000); builder.Property(x => x.ReplacementCondition).HasMaxLength(2000); builder.Property(x => x.RemovalCondition).HasMaxLength(2000);
        builder.HasMany(x => x.Transfers).WithOne().HasForeignKey(x => x.MeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SafetyResponsibilityTransfer>().WithMany().HasForeignKey(x => new { x.Id, x.CurrentResponsibilityTransferId })
            .HasPrincipalKey(x => new { x.MeasureId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Transfers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
