using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class BusinessReceivingRequestConfiguration : IEntityTypeConfiguration<BusinessReceivingRequest>,
    IEntityTypeConfiguration<BusinessDutyAppointment>
{
    public void Configure(EntityTypeBuilder<BusinessReceivingRequest> builder)
    {
        builder.ToTable("BusinessReceivingRequests", t =>
        {
            t.HasTrigger("TR_BusinessReceivingRequests_SourceAck");
            t.HasCheckConstraint("CK_BusinessReceivingRequests_Ack", "([AcknowledgedAt] IS NULL AND [AcknowledgmentId] IS NULL AND [AcknowledgedBy] IS NULL AND [ClockId] IS NULL AND [ClaimedDeviceAt] IS NULL) OR ([AcknowledgedAt] IS NOT NULL AND [AcknowledgmentId] IS NOT NULL AND [AcknowledgedBy] IS NOT NULL AND [ClockId] IS NOT NULL AND [AcknowledgedAt]>=[RequestedAt])");
        }); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SourceKind).HasMaxLength(64); builder.Property(x => x.SourceVersion).HasMaxLength(256);
        builder.Property(x => x.Kind).HasConversion<byte>(); builder.Property(x => x.ResponsibleRole).HasConversion<byte>();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.SourceKind, x.SourceId, x.Kind }).IsUnique();
        builder.HasIndex(x => new { x.ProjectId, x.ScopeId, x.CompletedAt });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResponsibleActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Clock).WithMany().HasForeignKey(x => x.ClockId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Appointments).WithOne().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Appointments).HasField("appointments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
    public void Configure(EntityTypeBuilder<BusinessDutyAppointment> builder)
    {
        builder.ToTable("BusinessDutyAppointments", t => t.HasTrigger("TR_BusinessDutyAppointments_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CurrentActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DecisionActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
