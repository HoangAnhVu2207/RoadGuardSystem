using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations.Worker;

public sealed class AiVideoWorkItemConfiguration : IEntityTypeConfiguration<AiVideoWorkItem>
{
    public void Configure(EntityTypeBuilder<AiVideoWorkItem> builder)
    {
        builder.ToTable("AiVideoWorkItems", table =>
        {
            table.HasCheckConstraint("CK_AiVideoWorkItems_State", "[State] IN (1,2,3,4,5,6,7,8)");
            table.HasCheckConstraint("CK_AiVideoWorkItems_Admission", "ISJSON([AdmissionJson]) = 1 AND LEFT([AdmissionJson], 1) = '{'");
            table.HasCheckConstraint("CK_AiVideoWorkItems_Lease", "([LeaseOwner] IS NULL AND [LeaseUntil] IS NULL) OR ([LeaseOwner] IS NOT NULL AND [LeaseUntil] IS NOT NULL)");
            table.HasCheckConstraint("CK_AiVideoWorkItems_Time", "[UpdatedAt] >= [CreatedAt]");
            table.HasCheckConstraint("CK_AiVideoWorkItems_Failure", "([State] IN (6,7) AND [ErrorCode] IS NOT NULL) OR ([State] NOT IN (6,7) AND [ErrorCode] IS NULL)");
            table.HasCheckConstraint("CK_AiVideoWorkItems_TerminalLease", "[State] IN (1,2,3,4) OR [LeaseOwner] IS NULL");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LogicalConfigurationIdentity).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.DispatchKey).HasColumnType("char(64)").IsUnicode(false).IsRequired();
        builder.Property(x => x.AdmissionJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.State).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(x => x.LeaseOwner).HasMaxLength(120).IsUnicode(false);
        builder.Property(x => x.ErrorCode).HasMaxLength(80).IsUnicode(false);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.Property(x => x.CreatedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.NextPollAt).HasColumnType("datetimeoffset(7)");
        builder.Property(x => x.LeaseUntil).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(x => x.DispatchKey).IsUnique().HasDatabaseName("UX_AiVideoWorkItems_DispatchKey");
        builder.HasIndex(x => x.ProcessingJobId).IsUnique().HasDatabaseName("UX_AiVideoWorkItems_Job");
        builder.HasIndex(x => new { x.State, x.NextPollAt }).HasDatabaseName("IX_AiVideoWorkItems_Due");
        builder.HasOne<SurveyDataVersion>().WithMany().HasForeignKey(x => x.DatasetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.SourceVideoFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingJob>().WithMany().HasForeignKey(x => x.ProcessingJobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProcessingAttempt>().WithMany().HasForeignKey(x => x.CurrentAttemptId).OnDelete(DeleteBehavior.Restrict);
        foreach (var name in new[] { nameof(AiVideoWorkItem.DatasetId), nameof(AiVideoWorkItem.SourceVideoFileId),
            nameof(AiVideoWorkItem.ProjectId), nameof(AiVideoWorkItem.ProcessingJobId), nameof(AiVideoWorkItem.DispatchKey),
            nameof(AiVideoWorkItem.LogicalConfigurationIdentity), nameof(AiVideoWorkItem.CreatedAt) })
            builder.Property(name).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
