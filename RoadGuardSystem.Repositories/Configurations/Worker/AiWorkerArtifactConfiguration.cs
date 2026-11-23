using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;

namespace RoadGuardSystem.Repositories.Configurations.Worker;

public sealed class AiWorkerArtifactConfiguration : IEntityTypeConfiguration<AiWorkerArtifact>
{
    public void Configure(EntityTypeBuilder<AiWorkerArtifact> builder)
    {
        builder.ToTable("AiWorkerArtifacts", table =>
        {
            table.HasTrigger("TR_AiWorkerArtifacts_Immutable");
            table.HasCheckConstraint("CK_AiWorkerArtifacts_Bytes", "[Bytes] > 0");
            table.HasCheckConstraint("CK_AiWorkerArtifacts_KindDimensions", "([Kind] = 'FRAME' AND [Width] BETWEEN 1 AND 16384 AND [Height] BETWEEN 1 AND 16384 AND [Width] IS NOT NULL AND [Height] IS NOT NULL) OR ([Kind] IN ('RAW_RESULT','TELEMETRY_SRT') AND [Width] IS NULL AND [Height] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ArtifactId).HasMaxLength(36).IsUnicode(false).IsRequired();
        builder.Property(x => x.Sha256).HasColumnType("char(64)").IsUnicode(false).IsRequired();
        builder.Property(x => x.Bytes).HasColumnType("bigint");
        builder.Property(x => x.Kind).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => new { x.WorkItemId, x.AttemptId, x.ArtifactId }).IsUnique().HasDatabaseName("UX_AiWorkerArtifacts_AttemptArtifact");
        builder.HasOne<AiVideoWorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        foreach (var name in new[] { nameof(AiWorkerArtifact.WorkItemId), nameof(AiWorkerArtifact.AttemptId),
            nameof(AiWorkerArtifact.ArtifactId), nameof(AiWorkerArtifact.Sha256), nameof(AiWorkerArtifact.Bytes),
            nameof(AiWorkerArtifact.Kind), nameof(AiWorkerArtifact.Width), nameof(AiWorkerArtifact.Height) })
            builder.Property(name).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
