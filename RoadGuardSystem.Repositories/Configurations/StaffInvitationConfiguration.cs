using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class StaffInvitationConfiguration : IEntityTypeConfiguration<StaffInvitation>
{
    public void Configure(EntityTypeBuilder<StaffInvitation> builder)
    {
        builder.ToTable("StaffInvitations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Email).HasMaxLength(254).IsRequired();
        builder.Property(item => item.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Property(item => item.RoleCode)
            .HasConversion(role => role.ToDbCode(), code => UserRoleCodeExtensions.FromDbCode(code))
            .HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(item => item.TokenHash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(item => item.ExpiresAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(item => item.AcceptedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(item => item.RevokedAt).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(item => item.TokenHash).IsUnique().HasDatabaseName("UX_StaffInvitations_TokenHash");
        builder.HasIndex(item => item.NormalizedEmail).HasDatabaseName("IX_StaffInvitations_Email");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(item => item.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
