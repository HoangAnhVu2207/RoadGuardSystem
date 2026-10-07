using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class WeeklyReviewDigestConfiguration : IEntityTypeConfiguration<WeeklyReviewRecoveryPeriod>,
    IEntityTypeConfiguration<WeeklyReviewDigest>, IEntityTypeConfiguration<WeeklyReviewDigestDuty>
{
    public void Configure(EntityTypeBuilder<WeeklyReviewRecoveryPeriod> builder)
    {
        builder.ToTable("WeeklyReviewRecoveryPeriods", t => t.HasTrigger("TR_WeeklyReviewRecoveryPeriods_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.ProjectId, x.ScheduledAtUtc }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<WeeklyReviewDigest> builder)
    {
        builder.ToTable("WeeklyReviewDigests", t => t.HasTrigger("TR_WeeklyReviewDigests_Immutable"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RecipientKey).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.RecipientRole).HasConversion<byte>();
        builder.HasIndex(x => new { x.ProjectId, x.ScheduledAtUtc, x.RecipientKey }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WeeklyReviewRecoveryPeriod>().WithMany().HasForeignKey(x => x.RecoveryPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Duties).WithOne().HasForeignKey(x => x.DigestId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<WeeklyReviewDigestDuty> builder)
    {
        builder.ToTable("WeeklyReviewDigestDuties", t => t.HasTrigger("TR_WeeklyReviewDigestDuties_Immutable"));
        builder.HasKey(x => new { x.DigestId, x.ClockId }); builder.Property(x => x.Kind).HasMaxLength(64).IsUnicode(false);
        builder.HasOne<DeadlineClock>().WithMany().HasForeignKey(x => x.ClockId).OnDelete(DeleteBehavior.Restrict);
    }
}
