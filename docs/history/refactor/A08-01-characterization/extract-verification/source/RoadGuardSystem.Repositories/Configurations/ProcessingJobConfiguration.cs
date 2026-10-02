using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("ProcessingJobs", table =>
        {
            table.HasCheckConstraint("CK_ProcessingJobs_Status", "[Status] IN (1, 2, 3, 4, 5, 6)");
            table.HasCheckConstraint(
                "CK_ProcessingJobs_TimestampOrder",
                "[CompletedAt] IS NULL OR [StartedAt] IS NULL OR [CompletedAt] >= [StartedAt]");
        });

        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(job => job.ProcessingBlockId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(job => job.ModelVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(job => job.ProjectId).HasColumnType("uniqueidentifier");
        builder.Property(job => job.ManifestHash).HasColumnType("char(64)").IsUnicode(false).HasDefaultValue("");
        builder.Property(job => job.ManifestJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}");
        builder.Property(job => job.Mode).HasMaxLength(8).IsUnicode(false).HasDefaultValue("");
        builder.Property(job => job.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.Property(job => job.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(job => job.StartedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(job => job.CompletedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(job => job.ErrorCode).HasMaxLength(80).IsUnicode(false);
        builder.Property(job => job.ErrorMessage).HasColumnType("nvarchar(max)");
        builder.HasIndex(job => job.ProcessingBlockId)
            .HasDatabaseName("IX_ProcessingJobs_Block");
        builder.HasIndex(job => job.ModelVersionId).HasDatabaseName("IX_ProcessingJobs_ModelVersionId");
        builder.HasIndex(job => new { job.ProjectId, job.Status }).HasDatabaseName("IX_ProcessingJobs_ProjectStatus");
        builder.HasOne<ProcessingBlock>()
            .WithMany()
            .HasForeignKey(job => job.ProcessingBlockId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIModelVersion>()
            .WithMany()
            .HasForeignKey(job => job.ModelVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
