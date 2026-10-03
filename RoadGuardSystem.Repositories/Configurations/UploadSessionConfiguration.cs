using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.ToTable("UploadSessions", table =>
        {
            table.HasCheckConstraint("CK_UploadSessions_ExpectedSizeBytes_Positive", "[ExpectedSizeBytes] > 0");
            table.HasCheckConstraint("CK_UploadSessions_PartSizeBytes_Positive", "[PartSizeBytes] > 0");
        });
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(session => session.ObjectKey).HasMaxLength(512).IsUnicode(false).IsRequired();
        builder.HasIndex(session => session.ObjectKey).IsUnique().HasDatabaseName("UX_UploadSessions_ObjectKey");
        builder.Property(session => session.Purpose).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(session => session.MediaType).HasMaxLength(120).IsUnicode(false).IsRequired();
        builder.Property(session => session.ExpectedSizeBytes).HasColumnType("bigint").IsRequired();
        builder.Property(session => session.ExpectedChecksumSha256).HasColumnType("char(64)").IsRequired();
        builder.Property(session => session.StorageUploadId).HasMaxLength(1024).IsUnicode(false);
        builder.Property(session => session.FailureCode).HasMaxLength(80).IsUnicode(false);
        builder.Property(session => session.MultipartPhase).HasMaxLength(20).IsUnicode(false);
        builder.HasIndex(session => session.MultipartNextCheckAt).HasDatabaseName("IX_UploadSessions_MultipartNextCheckAt");
        builder.Property(session => session.ExpiresAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.HasIndex(session => new { session.Status, session.ExpiresAt }).HasDatabaseName("IX_UploadSessions_Status_ExpiresAt");
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(session => session.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(session => session.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
