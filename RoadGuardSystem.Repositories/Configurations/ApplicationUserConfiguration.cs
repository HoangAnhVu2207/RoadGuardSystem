using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint("CK_Users_Status", "[Status] IN (1, 2, 3)");
        });

        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(user => user.UserName)
            .IsUnique()
            .HasDatabaseName("UX_Users_UserName");

        builder.Property(user => user.NormalizedUserName)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(user => user.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("UX_Users_NormalizedUserName");

        builder.Property(user => user.Email)
            .HasMaxLength(254);
        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL")
            .HasDatabaseName("UX_Users_Email");

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(254);

        builder.Property(user => user.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(user => user.RoleCode)
            .HasConversion(
                role => role.ToDbCode(),
                dbCode => UserRoleCodeExtensions.FromDbCode(dbCode))
            .HasMaxLength(40)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne(user => user.Role)
            .WithMany()
            .HasForeignKey(user => user.RoleCode)
            .HasPrincipalKey(role => role.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(user => user.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(user => user.MustChangePassword)
            .IsRequired();

        builder.Property(user => user.LastLoginAt)
            .HasColumnType("datetimeoffset(7)");

        builder.Property(user => user.SuspendedAt)
            .HasColumnType("datetimeoffset(7)");

        builder.Property(user => user.CreatedAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();

        builder.Property(user => user.ConcurrencyStamp)
            .HasMaxLength(36);

        builder.Property(user => user.SecurityStamp)
            .HasMaxLength(36);

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(50);
    }
}
