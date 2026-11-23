using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations.Worker;

public sealed class AiVideoTelemetryRevisionConfiguration : IEntityTypeConfiguration<AiVideoTelemetryRevision>
{
    public void Configure(EntityTypeBuilder<AiVideoTelemetryRevision> builder)
    {
        builder.ToTable("AiVideoTelemetryRevisions", t => {
            t.HasTrigger("TR_AiVideoTelemetryRevisions_AppendOnly");
            t.HasCheckConstraint("CK_AiVideoTelemetryRevisions_Number", "([Number]=1 AND [PreviousId] IS NULL) OR ([Number]>1 AND [PreviousId] IS NOT NULL)");
            t.HasCheckConstraint("CK_AiVideoTelemetryRevisions_Json", "ISJSON([IntervalsJson])=1");
            t.HasCheckConstraint("CK_AiVideoTelemetryRevisions_Kind", "([Kind]='EXTERNAL_SRT' AND [OriginalSrtObjectKey] IS NOT NULL AND [OriginalSrtSha256] IS NOT NULL AND [OriginalSrtBytes] BETWEEN 1 AND 2097152) OR ([Kind]='OPERATOR_ESTIMATE' AND [OriginalSrtObjectKey] IS NULL AND [OriginalSrtSha256] IS NULL AND [OriginalSrtBytes] IS NULL AND [Note] IS NOT NULL AND [OffsetMs]=0)");
            t.HasCheckConstraint("CK_AiVideoTelemetryRevisions_Offset", "[OffsetMs] BETWEEN -86400000 AND 86400000");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.Kind).HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.IntervalsJson).IsRequired();
        builder.Property(x => x.OriginalSrtObjectKey).HasMaxLength(512);
        builder.Property(x => x.Note).HasMaxLength(1000);
        foreach (var field in new[] { "SourceVideoHash", "OriginalSrtSha256", "IdempotencyKeyHash", "PayloadFingerprint" })
            builder.Property<string>(field).HasColumnType("char(64)").IsUnicode(false);
        builder.HasIndex(x => new { x.WorkId, x.Number }).IsUnique();
        builder.HasIndex(x => new { x.WorkId, x.ActorId, x.IdempotencyKeyHash }).IsUnique();
        builder.HasOne<AiVideoWorkItem>().WithMany().HasForeignKey(x => x.WorkId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiVideoTelemetryRevision>().WithMany().HasForeignKey(x => x.PreviousId).OnDelete(DeleteBehavior.Restrict);
        foreach (var property in builder.Metadata.GetProperties()) property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}

\n