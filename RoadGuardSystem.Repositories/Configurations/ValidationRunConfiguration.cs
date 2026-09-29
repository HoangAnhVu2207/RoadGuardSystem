using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ValidationRunConfiguration : IEntityTypeConfiguration<ValidationRun>
{
    public void Configure(EntityTypeBuilder<ValidationRun> builder)
    {
        builder.ToTable("ValidationRuns", table =>
        {
            table.HasCheckConstraint("CK_ValidationRuns_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_ValidationRuns_PairsJson", "ISJSON([PairsJson]) = 1 AND LEFT(LTRIM([PairsJson]), 1) = '['");
            table.HasCheckConstraint("CK_ValidationRuns_ExclusionReasonsJson", "ISJSON([ExclusionReasonsJson]) = 1 AND LEFT(LTRIM([ExclusionReasonsJson]), 1) = '['");
        });
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(run => run.ProjectId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(run => run.ModelVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(run => run.DatasetSplitId).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(run => run.MeasurementType).HasMaxLength(80).IsUnicode(false).IsRequired();
        builder.Property(run => run.Unit).HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(run => run.PairsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(run => run.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(run => run.UsedCount).HasColumnType("int").IsRequired();
        builder.Property(run => run.ExcludedCount).HasColumnType("int").IsRequired();
        builder.Property(run => run.Bias).HasColumnType("decimal(18,6)");
        builder.Property(run => run.Mae).HasColumnType("decimal(18,6)");
        builder.Property(run => run.Rmse).HasColumnType("decimal(18,6)");
        builder.Property(run => run.ExclusionReasonsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(run => run.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(run => new { run.ProjectId, run.Status }).HasDatabaseName("IX_ValidationRuns_ProjectStatus");
        builder.HasOne<AIModelVersion>().WithMany().HasForeignKey(run => run.ModelVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
