using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class HandoverDocumentConfiguration : IEntityTypeConfiguration<HandoverDocument>
{
    public void Configure(EntityTypeBuilder<HandoverDocument> builder)
    {
        builder.ToTable("HandoverDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(document => document.DocumentNo).HasMaxLength(80).IsUnicode(false).IsRequired();
        builder.Property(document => document.HandoverDate).HasColumnType("date").IsRequired();
        builder.Property(document => document.Notes).HasColumnType("nvarchar(max)");
        builder.HasIndex(document => new { document.ProjectId, document.DocumentNo })
            .IsUnique()
            .HasDatabaseName("UX_HandoverDocuments_ProjectId_DocumentNo");
        builder.HasOne<Project>().WithMany().HasForeignKey(document => document.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(document => document.AcceptedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(document => document.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}
