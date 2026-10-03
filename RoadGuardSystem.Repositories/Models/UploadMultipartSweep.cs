using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;

namespace RoadGuardSystem.Repositories.Models;

// Scheduling only; session/fence remains the authority. A separate row avoids ETag drift on verified files.
public sealed class UploadMultipartSweep
{
    public Guid UploadSessionId { get; set; }
    public DateTimeOffset NextCheckAt { get; set; }
}

public sealed class UploadMultipartSweepConfiguration : IEntityTypeConfiguration<UploadMultipartSweep>
{
    public void Configure(EntityTypeBuilder<UploadMultipartSweep> builder)
    {
        builder.ToTable("UploadMultipartSweeps");
        builder.HasKey(s => s.UploadSessionId);
        builder.HasOne<UploadSession>().WithOne().HasForeignKey<UploadMultipartSweep>(s => s.UploadSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.NextCheckAt);
    }
}
