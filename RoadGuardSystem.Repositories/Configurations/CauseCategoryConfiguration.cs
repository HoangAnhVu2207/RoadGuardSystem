using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class CauseCategoryConfiguration : IEntityTypeConfiguration<CauseCategory>
{
    public void Configure(EntityTypeBuilder<CauseCategory> builder)
    {
        builder.ToTable("CauseCategories");
        builder.HasKey(category => category.Code);
        builder.Property(category => category.Code)
            .HasMaxLength(80)
            .IsUnicode(false)
            .ValueGeneratedNever();
        builder.Property(category => category.Name)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(category => category.IsActive)
            .HasColumnType("bit")
            .IsRequired();
    }
}
