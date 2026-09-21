using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class SurveyFileConfiguration : IEntityTypeConfiguration<SurveyFile>
{
    public void Configure(EntityTypeBuilder<SurveyFile> builder)
    {
        builder.ToTable("SurveyFiles", table =>
        {
            table.HasTrigger("TR_SurveyFiles_ScopeIntegrity");
            table.HasCheckConstraint("CK_SurveyFiles_FileType", "[FileType] IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_SurveyFiles_SyncStatus", "[SyncStatus] IN (1, 2, 3, 4, 5)");
            table.HasCheckConstraint("CK_SurveyFiles_TimestampOrder", "[CaptureEndedAt] IS NULL OR [CaptureStartedAt] IS NULL OR [CaptureEndedAt] >= [CaptureStartedAt]");
            table.HasCheckConstraint(
                "CK_SurveyFiles_Checksum_Sha256Lowercase",
                "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
        });

        builder.HasKey(file => file.Id);
        builder.Property(file => file.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(file => file.SurveyId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(file => file.FlightId).HasColumnType("uniqueidentifier");
        builder.Property(file => file.FileId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(file => file.FileType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(file => file.CaptureStartedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(file => file.CaptureEndedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(file => file.SyncStatus).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(file => file.Checksum).HasColumnType("char(64)").IsRequired();
        builder.HasIndex(file => file.SurveyId).HasDatabaseName("IX_SurveyFiles_SurveyId");
        builder.HasIndex(file => file.FlightId).HasDatabaseName("IX_SurveyFiles_FlightId");
        builder.HasIndex(file => file.FileId).HasDatabaseName("IX_SurveyFiles_FileId");
        builder.HasOne<Survey>().WithMany().HasForeignKey(file => file.SurveyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Flight>().WithMany().HasForeignKey(file => file.FlightId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(file => file.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
