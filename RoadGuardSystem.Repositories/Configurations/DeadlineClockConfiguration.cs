using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DeadlineClockConfiguration : IEntityTypeConfiguration<DeadlineClock>,
    IEntityTypeConfiguration<DeadlineExtension>, IEntityTypeConfiguration<DeadlineBreach>
{
    public void Configure(EntityTypeBuilder<DeadlineClock> builder)
    {
        builder.ToTable("DeadlineClocks", table =>
        {
            table.HasTrigger("TR_DeadlineClocks_ImmutableOrigin");
            table.HasCheckConstraint("CK_DeadlineClocks_Kind", "[Kind] BETWEEN 1 AND 10");
            table.HasCheckConstraint("CK_DeadlineClocks_Times", "([OriginalDueAt]>[OriginAt] OR ([Kind]=10 AND [OriginalDueAt]=[OriginAt])) AND [CurrentDueAt]>=[OriginalDueAt] AND ([CompletedAt] IS NULL OR [CompletedAt]>=[OriginAt])");
            table.HasCheckConstraint("CK_DeadlineClocks_Acknowledgment", "([AcknowledgedAt] IS NULL AND [AcknowledgedByUserId] IS NULL AND [AcknowledgmentEventId] IS NULL) OR ([Kind]=9 AND [AcknowledgedAt] IS NOT NULL AND [AcknowledgedAt]>=[OriginAt] AND [AcknowledgedByUserId] IS NOT NULL AND [AcknowledgmentEventId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [CompletedAt]=[AcknowledgedAt])");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.Kind, x.TargetId }).IsUnique();
        builder.HasIndex(x => new { x.ProjectId, x.CompletedAt, x.CurrentDueAt });
        builder.HasIndex(x => x.OriginEventId);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AcknowledgedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Extensions).WithOne().HasForeignKey(x => x.ClockId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Extensions).HasField("_extensions").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Breaches).WithOne().HasForeignKey(x => x.ClockId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Breaches).HasField("_breaches").UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    public void Configure(EntityTypeBuilder<DeadlineExtension> builder)
    {
        builder.ToTable("DeadlineExtensions", table =>
        {
            table.HasTrigger("TR_DeadlineExtensions_Immutable");
            table.HasCheckConstraint("CK_DeadlineExtensions_Times", "[NewDueAt]>[PreviousDueAt] AND [NewDueAt]>[OccurredAt]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.ClockId, x.NewDueAt }).IsUnique();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DeadlineBreach> builder)
    {
        builder.ToTable("DeadlineBreaches", table =>
        {
            table.HasTrigger("TR_DeadlineBreaches_Immutable");
            table.HasCheckConstraint("CK_DeadlineBreaches_Times", "[ObservedAt]>=[DueAt]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.ClockId, x.DueAt }).IsUnique();
    }
}
