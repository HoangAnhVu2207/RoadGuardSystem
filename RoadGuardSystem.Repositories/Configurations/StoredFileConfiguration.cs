using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("Files", table =>
        {
            table.HasTrigger("TR_Files_Immutable");
            table.HasCheckConstraint("CK_Files_SizeBytes_NonNegative", "[SizeBytes] >= 0");
            table.HasCheckConstraint(
                "CK_Files_Checksum_Sha256Lowercase",
                "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
        });

        builder.HasKey(file => file.Id);
        builder.Property(file => file.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(file => file.StorageUri).HasMaxLength(2048).IsRequired();
        builder.HasIndex(file => file.StorageUri).IsUnique().HasDatabaseName("UX_Files_StorageUri");
        builder.Property(file => file.OriginalName).HasMaxLength(255).IsUnicode(false).IsRequired();
        builder.Property(file => file.MimeType).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(file => file.SizeBytes).HasColumnType("int").IsRequired();
        builder.Property(file => file.Checksum).HasColumnType("char(64)").IsRequired();
        builder.Property(file => file.UploadedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(file => file.RetentionUntil).HasColumnType("date");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(file => file.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
