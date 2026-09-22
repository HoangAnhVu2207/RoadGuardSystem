using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ProcessingBlockConfiguration : IEntityTypeConfiguration<ProcessingBlock>
{
    public void Configure(EntityTypeBuilder<ProcessingBlock> builder)
    {
        builder.ToTable("ProcessingBlocks", table =>
        {
            table.HasTrigger("TR_ProcessingBlocks_Immutable");
            table.HasCheckConstraint("CK_ProcessingBlocks_BlockNo", "[BlockNo] > 0");
            table.HasCheckConstraint(
                "CK_ProcessingBlocks_RangeMetadata_JsonObject",
                "ISJSON([RangeMetadata]) = 1 AND LEFT(LTRIM([RangeMetadata]), 1) = '{'");
        });

        builder.HasKey(block => block.Id);
        builder.Property(block => block.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(block => block.SurveyDataVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(block => block.BlockNo).HasColumnType("int").IsRequired();
        builder.Property(block => block.RangeMetadata).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(block => new { block.SurveyDataVersionId, block.BlockNo })
            .IsUnique()
            .HasDatabaseName("UX_ProcessingBlocks_DataVersionBlockNo");
        builder.HasOne<SurveyDataVersion>()
            .WithMany()
            .HasForeignKey(block => block.SurveyDataVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(block => block.SurveyDataVersionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(block => block.BlockNo).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(block => block.RangeMetadata).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
