using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class Anh02ExportConfiguration : IEntityTypeConfiguration<ExportJob>, IEntityTypeConfiguration<ExportSnapshot>, IEntityTypeConfiguration<ExportSnapshotFile>, IEntityTypeConfiguration<GeneratedArtifact>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        var b = builder;
        b.ToTable("Anh02ExportJobs", t =>
        {
            t.HasCheckConstraint("CK_Anh02ExportJob_Kind", "[Kind] IN ('DOSSIER','TRAINING')");
            t.HasCheckConstraint("CK_Anh02ExportJob_Format", "[Format] IN ('PDF','ZIP') AND ([Kind] <> 'TRAINING' OR [Format]='ZIP')");
            t.HasCheckConstraint("CK_Anh02ExportJob_State", "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')");
            t.HasCheckConstraint("CK_Anh02ExportJob_Expiry", "([Status] <> 'SUCCEEDED' AND [ExpiresAt] IS NULL) OR ([Status]='SUCCEEDED' AND [ArtifactId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [ExpiresAt]=DATEADD(day,30,[CompletedAt]))");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Kind).HasMaxLength(16).IsUnicode(false); b.Property(x => x.Format).HasMaxLength(8).IsUnicode(false);
        b.Property(x => x.Status).HasMaxLength(16).IsUnicode(false); b.Property(x => x.ErrorCode).HasMaxLength(80).IsUnicode(false);
        b.Property(x => x.RowVersion).IsRowVersion(); b.HasIndex(x => new { x.Status, x.NextAttemptAt, x.LeaseUntil });
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ExportSnapshot>().WithMany().HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.SnapshotId).IsUnique();
        b.HasOne<GeneratedArtifact>().WithMany().HasForeignKey(x => x.ArtifactId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ExportSnapshot> builder)
    {
        var b = builder;
        b.ToTable("Anh02ExportSnapshots", t => t.HasTrigger("TR_Anh02ExportSnapshots_Immutable")); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired(); b.Property(x => x.Hash).HasColumnType("char(64)").IsRequired();
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ExportSnapshotFile> builder)
    {
        var b = builder;
        b.ToTable("Anh02ExportSnapshotFiles", t => { t.HasTrigger("TR_Anh02ExportSnapshotFiles_Immutable"); t.HasCheckConstraint("CK_Anh02ExportSnapshotFile_Size", "[SizeBytes]>0"); }); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.FileVersion).HasMaxLength(128); b.Property(x => x.Sha256).HasColumnType("char(64)");
        b.Property(x => x.MediaType).HasMaxLength(120); b.Property(x => x.ArchivePath).HasMaxLength(255);
        b.HasIndex(x => new { x.SnapshotId, x.FileId }).IsUnique();
        b.HasOne<ExportSnapshot>().WithMany().HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<GeneratedArtifact> builder)
    {
        var b = builder;
        b.ToTable("Anh02GeneratedArtifacts", t => t.HasTrigger("TR_Anh02GeneratedArtifacts_Immutable")); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.ExportJobId).IsUnique(); b.HasIndex(x => x.FileId).IsUnique();
        b.HasOne<ExportJob>().WithMany().HasForeignKey(x => x.ExportJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ExportSnapshot>().WithMany().HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
