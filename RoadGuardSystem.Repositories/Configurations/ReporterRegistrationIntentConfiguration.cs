using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ReporterRegistrationIntentConfiguration : IEntityTypeConfiguration<ReporterRegistrationIntent>
{
    public void Configure(EntityTypeBuilder<ReporterRegistrationIntent> builder)
    {
        builder.ToTable("ReporterRegistrationIntents", table =>
        {
            table.HasCheckConstraint("CK_ReporterRegistrationIntents_ReporterType", "[ReporterType] IN (1, 2)");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Property(item => item.ReporterType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(item => item.OtpHash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.ExpiresAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(item => item.ResendAvailableAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(item => item.ConsumedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(item => item.EmailConfirmedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(item => item.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.HasIndex(item => item.NormalizedEmail).HasDatabaseName("IX_ReporterRegistrationIntents_Email");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
