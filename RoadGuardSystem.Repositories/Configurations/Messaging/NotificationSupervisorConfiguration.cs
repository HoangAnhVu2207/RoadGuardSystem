using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Configurations.Messaging;

public sealed class NotificationSupervisorHeadConfiguration : IEntityTypeConfiguration<NotificationSupervisorHead>
{
    public void Configure(EntityTypeBuilder<NotificationSupervisorHead> builder)
    {
        builder.ToTable("NotificationSupervisorHeads"); builder.HasKey(x => x.ItemId);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.AssignmentId).IsUnique();
        builder.HasOne<RepairItem>().WithOne().HasForeignKey<NotificationSupervisorHead>(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class NotificationSupervisorGenerationConfiguration : IEntityTypeConfiguration<NotificationSupervisorGeneration>
{
    public void Configure(EntityTypeBuilder<NotificationSupervisorGeneration> builder)
    {
        builder.ToTable("NotificationSupervisorGenerations", t => t.HasTrigger("TR_NotificationSupervisorGenerations_Immutable")); builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).HasMaxLength(12).IsUnicode(false).IsRequired();
        builder.HasIndex(x => new { x.ItemId, x.Version }).IsUnique();
        builder.HasOne<NotificationSupervisorHead>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class NotificationSupervisorAuditConfiguration : IEntityTypeConfiguration<NotificationSupervisorAudit>
{
    public void Configure(EntityTypeBuilder<NotificationSupervisorAudit> builder)
    {
        builder.ToTable("NotificationSupervisorAudits", t => t.HasTrigger("TR_NotificationSupervisorAudits_Immutable")); builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.HasOne<NotificationSupervisorGeneration>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
