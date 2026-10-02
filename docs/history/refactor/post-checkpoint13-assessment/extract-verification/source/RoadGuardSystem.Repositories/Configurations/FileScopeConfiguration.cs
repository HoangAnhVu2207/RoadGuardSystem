using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class FileScopeConfiguration : IEntityTypeConfiguration<FileScope>
{
    public void Configure(EntityTypeBuilder<FileScope> builder)
    {
        builder.ToTable("FileScopes");
        builder.HasKey(scope => scope.Id);
        builder.Property(scope => scope.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(scope => scope.Purpose).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(scope => scope.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.HasIndex(scope => scope.FileId).IsUnique().HasDatabaseName("UX_FileScopes_FileId");
        builder.HasIndex(scope => new { scope.ProjectId, scope.OwnerUserId }).HasDatabaseName("IX_FileScopes_ProjectId_OwnerUserId");
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(scope => scope.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(scope => scope.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(scope => scope.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
