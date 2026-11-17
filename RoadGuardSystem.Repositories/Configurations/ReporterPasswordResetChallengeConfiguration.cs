using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ReporterPasswordResetChallengeConfiguration : IEntityTypeConfiguration<ReporterPasswordResetChallenge>
{
    public void Configure(EntityTypeBuilder<ReporterPasswordResetChallenge> builder)
    {
        builder.ToTable("ReporterPasswordResetChallenges");
        builder.HasKey(row => row.UserId);
        builder.Property(row => row.OtpHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
