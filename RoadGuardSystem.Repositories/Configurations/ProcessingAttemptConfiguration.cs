using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ProcessingAttemptConfiguration : IEntityTypeConfiguration<ProcessingAttempt>
{
    public void Configure(EntityTypeBuilder<ProcessingAttempt> builder)
    {
        builder.ToTable("ProcessingAttempts", table =>
        {
            table.HasTrigger("TR_ProcessingAttempts_AppendOnly");
            table.HasCheckConstraint("CK_ProcessingAttempts_AttemptNo", "[AttemptNo] > 0");
            table.HasCheckConstraint("CK_ProcessingAttempts_ErrorType", "[ErrorType] IS NULL OR [ErrorType] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_ProcessingAttempts_TimestampOrder",
                "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
        });

        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(attempt => attempt.ProcessingJobId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(attempt => attempt.AttemptNo).HasColumnType("int").IsRequired();
        builder.Property(attempt => attempt.StartedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(attempt => attempt.EndedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(attempt => attempt.ErrorType).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(attempt => attempt.WorkerReference).HasMaxLength(120).IsUnicode(false);
        builder.HasIndex(attempt => new { attempt.ProcessingJobId, attempt.AttemptNo })
            .IsUnique()
            .HasDatabaseName("UX_ProcessingAttempts_JobAttemptNo");
        builder.HasIndex(attempt => attempt.ProcessingJobId).HasDatabaseName("IX_ProcessingAttempts_JobId");
        builder.HasOne<ProcessingJob>()
            .WithMany()
            .HasForeignKey(attempt => attempt.ProcessingJobId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(attempt => attempt.ProcessingJobId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(attempt => attempt.AttemptNo).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(attempt => attempt.StartedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(attempt => attempt.EndedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(attempt => attempt.ErrorType).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(attempt => attempt.WorkerReference).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
