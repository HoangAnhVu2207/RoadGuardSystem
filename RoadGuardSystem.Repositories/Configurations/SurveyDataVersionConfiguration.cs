using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyDataVersionConfiguration : IEntityTypeConfiguration<SurveyDataVersion>
{
    public void Configure(EntityTypeBuilder<SurveyDataVersion> builder)
    {
        builder.ToTable("SurveyDataVersions", table =>
        {
            table.HasTrigger("TR_SurveyDataVersions_Immutable");
            table.HasCheckConstraint("CK_SurveyDataVersions_VersionNo", "[VersionNo] > 0");
            table.HasCheckConstraint("CK_SurveyDataVersions_Status", "[Status] IN (1, 2, 3, 4, 5)");
            table.HasCheckConstraint("CK_SurveyDataVersions_IntegrityStatus", "[IntegrityStatus] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_SurveyDataVersions_SourceManifest_JsonArray", "ISJSON([SourceManifest]) = 1 AND LEFT(LTRIM([SourceManifest]), 1) = '['");
            table.HasCheckConstraint(
                "CK_SurveyDataVersions_ServerConfirmation",
                "([Status] = 3 AND [IntegrityStatus] = 2 AND [ConfirmedAt] IS NOT NULL AND [ConfirmedBy] = 1) " +
                "OR ([Status] <> 3 AND [ConfirmedAt] IS NULL AND [ConfirmedBy] IS NULL)");
        });

        builder.HasKey(version => version.Id);
        builder.Property(version => version.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(version => version.SurveyId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(version => version.VersionNo).HasColumnType("int").IsRequired();
        builder.Property(version => version.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(version => version.IntegrityStatus).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(version => version.ConfirmedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(version => version.ConfirmedBy).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(version => version.SourceManifest).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(version => new { version.SurveyId, version.VersionNo }).IsUnique()
            .HasDatabaseName("UX_SurveyDataVersions_SurveyVersion");
        builder.HasOne<Survey>().WithMany().HasForeignKey(version => version.SurveyId).OnDelete(DeleteBehavior.Restrict);
    }
}
