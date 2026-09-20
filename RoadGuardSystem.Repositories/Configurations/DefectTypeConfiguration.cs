using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DefectTypeConfiguration : IEntityTypeConfiguration<DefectType>
{
    public void Configure(EntityTypeBuilder<DefectType> builder)
    {
        builder.ToTable("DefectTypes");
        builder.HasKey(defectType => defectType.Code);
        builder.Property(defectType => defectType.Code)
            .HasMaxLength(80)
            .IsUnicode(false)
            .ValueGeneratedNever();
        builder.Property(defectType => defectType.Name)
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(defectType => defectType.Description)
            .HasColumnType("nvarchar(max)");
        builder.Property(defectType => defectType.IsActive)
            .HasColumnType("bit")
            .IsRequired();
    }
}
