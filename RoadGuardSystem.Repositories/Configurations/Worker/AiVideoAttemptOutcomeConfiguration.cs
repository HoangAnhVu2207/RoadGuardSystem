using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;

namespace RoadGuardSystem.Repositories.Configurations.Worker;

public sealed class AiVideoAttemptOutcomeConfiguration : IEntityTypeConfiguration<AiVideoAttemptOutcome>
{
    public void Configure(EntityTypeBuilder<AiVideoAttemptOutcome> builder)
    {
        builder.ToTable("AiVideoAttemptOutcomes", table =>
        {
            table.HasTrigger("TR_AiVideoAttemptOutcomes_AppendOnly");
            table.HasCheckConstraint("CK_AiVideoAttemptOutcomes_StateError", "([State] IN (5,8) AND [ErrorCode] IS NULL) OR ([State] IN (6,7) AND [ErrorCode] IS NOT NULL AND LEN([ErrorCode]) BETWEEN 1 AND 80 AND [ErrorCode] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9_]%')");
            table.HasCheckConstraint("CK_AiVideoAttemptOutcomes_AdmissionHash", "LEN([AdmissionHash])=64 AND [AdmissionHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
        });
        builder.HasKey(x => x.AttemptId);
        builder.Property(x => x.AttemptId).ValueGeneratedNever();
        builder.Property(x => x.State).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(x => x.ErrorCode).HasMaxLength(80).IsUnicode(false);
        builder.Property(x => x.AdmissionHash).HasColumnType("char(64)").IsUnicode(false).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(x => new { x.WorkId, x.OccurredAt });
        builder.HasOne<ProcessingAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiVideoWorkItem>().WithMany().HasForeignKey(x => x.WorkId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingJob>().WithMany().HasForeignKey(x => x.ProcessingJobId).OnDelete(DeleteBehavior.Restrict);
        foreach (var property in builder.Metadata.GetProperties()) property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
