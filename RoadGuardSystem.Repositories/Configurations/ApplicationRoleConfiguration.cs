using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(role => role.Code);
        builder.Property(role => role.Code)
            .HasConversion(
                role => role.ToDbCode(),
                dbCode => UserRoleCodeExtensions.FromDbCode(dbCode))
            .HasColumnName("Code")
            .HasMaxLength(40)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(role => role.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.NormalizedName)
            .HasMaxLength(100);

        builder.Property(role => role.ConcurrencyStamp)
            .HasMaxLength(36);

        builder.Property(role => role.IsActive)
            .IsRequired();
    }
}
