using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Processing;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class AIModelVersionConfiguration : IEntityTypeConfiguration<AIModelVersion>
{
    public void Configure(EntityTypeBuilder<AIModelVersion> builder)
    {
        builder.ToTable("AIModelVersions", table =>
        {
            table.HasCheckConstraint("CK_AIModelVersions_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_AIModelVersions_ReleaseMetadata",
                "([ReleasedAt] IS NULL AND [ReleasedByUserId] IS NULL) OR ([ReleasedAt] IS NOT NULL AND [ReleasedByUserId] IS NOT NULL)");
            table.HasCheckConstraint("CK_AIModelVersions_Metrics_Json", "[Metrics] IS NULL OR ISJSON([Metrics]) = 1");
            table.HasCheckConstraint(
                "CK_AIModelVersions_OperatingThresholds_Json",
                "[OperatingThresholds] IS NULL OR ISJSON([OperatingThresholds]) = 1");
        });

        builder.HasKey(model => model.Id);
        builder.Property(model => model.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(model => model.ModelName).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(model => model.VersionLabel).HasMaxLength(80).IsUnicode(false).IsRequired();
        builder.Property(model => model.ArtifactUri).HasMaxLength(2048).IsRequired();
        builder.Property(model => model.Metrics).HasColumnType("nvarchar(max)");
        builder.Property(model => model.OperatingThresholds).HasColumnType("nvarchar(max)");
        builder.Property(model => model.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(model => model.ReleasedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(model => model.ReleasedByUserId).HasColumnType("uniqueidentifier");
        builder.HasIndex(model => model.VersionLabel)
            .IsUnique()
            .HasDatabaseName("UX_AIModelVersions_VersionLabel");
        builder.HasIndex(model => model.ModelName).HasDatabaseName("IX_AIModelVersions_ModelName");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(model => model.ReleasedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
