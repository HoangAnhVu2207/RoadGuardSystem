using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class UploadPartConfiguration : IEntityTypeConfiguration<UploadPart>
{
    public void Configure(EntityTypeBuilder<UploadPart> builder)
    {
        builder.ToTable("UploadParts");
        builder.HasKey(part => part.Id);
        builder.Property(part => part.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(part => part.ETag).HasMaxLength(512).IsUnicode(false);
        builder.Property(part => part.UrlIssuedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(part => part.UrlExpiresAt).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(part => new { part.UploadSessionId, part.PartNumber }).IsUnique().HasDatabaseName("UX_UploadParts_Session_PartNumber");
        builder.HasOne<UploadSession>().WithMany().HasForeignKey(part => part.UploadSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
