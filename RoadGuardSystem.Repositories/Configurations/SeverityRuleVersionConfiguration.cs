using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SeverityRuleVersionConfiguration : IEntityTypeConfiguration<SeverityRuleVersion>
{
    public void Configure(EntityTypeBuilder<SeverityRuleVersion> builder)
    {
        builder.ToTable("SeverityRuleVersions", table =>
        {
            table.HasCheckConstraint("CK_SeverityRuleVersions_VersionNo_Positive", "[VersionNo] > 0");
            table.HasCheckConstraint(
                "CK_SeverityRuleVersions_EffectiveDateRange",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
            table.HasCheckConstraint(
                "CK_SeverityRuleVersions_RuleDefinition_JsonObject",
                "ISJSON([RuleDefinition]) = 1 AND LEFT(LTRIM([RuleDefinition]), 1) = '{'");
        });

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(rule => rule.StandardCode)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(rule => rule.RoadTypeCode)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(rule => rule.VersionNo)
            .HasColumnType("int")
            .IsRequired();
        builder.Property(rule => rule.RuleDefinition)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.Property(rule => rule.EffectiveFrom)
            .HasColumnType("date")
            .IsRequired();
        builder.Property(rule => rule.EffectiveTo)
            .HasColumnType("date");
        builder.HasIndex(rule => new { rule.StandardCode, rule.RoadTypeCode, rule.VersionNo })
            .IsUnique()
            .HasDatabaseName("UX_SeverityRuleVersions_ScopeVersion");

        builder.Property(rule => rule.StandardCode).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(rule => rule.RoadTypeCode).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(rule => rule.VersionNo).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(rule => rule.RuleDefinition).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(rule => rule.EffectiveFrom).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(rule => rule.EffectiveTo).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
