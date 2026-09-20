using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers", table =>
        {
            table.HasCheckConstraint("CK_ProjectMembers_Status", "[Status] IN (1, 2)");
            table.HasCheckConstraint(
                "CK_ProjectMembers_PrimaryRole",
                "[IsPrimary] = 0 OR [RoleCode] = 'PM'");
            table.HasCheckConstraint(
                "CK_ProjectMembers_EffectiveDateRange",
                "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
        });

        builder.HasKey(member => member.Id);
        builder.Property(member => member.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(member => member.RoleCode)
            .HasConversion(
                role => role.ToDbCode(),
                dbCode => UserRoleCodeExtensions.FromDbCode(dbCode))
            .HasMaxLength(40)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(member => member.ValidFrom).HasColumnType("date").IsRequired();
        builder.Property(member => member.ValidTo).HasColumnType("date");
        builder.Property(member => member.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.HasIndex(member => new { member.ProjectId, member.UserId })
            .HasDatabaseName("IX_ProjectMembers_ProjectId_UserId");
        builder.HasIndex(member => member.ProjectId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1 AND [Status] = 1")
            .HasDatabaseName("UX_ProjectMembers_ActivePrimaryProjectManager");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationRole>()
            .WithMany()
            .HasForeignKey(member => member.RoleCode)
            .HasPrincipalKey(role => role.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
