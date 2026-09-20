using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DefectConfiguration : IEntityTypeConfiguration<Defect>
{
    public void Configure(EntityTypeBuilder<Defect> builder)
    {
        builder.ToTable("Defects");
        builder.HasKey(defect => defect.Id);
        builder.Property(defect => defect.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(defect => defect.DefectTypeCode)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(defect => defect.CauseCategoryCode)
            .HasMaxLength(80)
            .IsUnicode(false);

        builder.HasOne<DefectType>()
            .WithMany()
            .HasForeignKey(defect => defect.DefectTypeCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CauseCategory>()
            .WithMany()
            .HasForeignKey(defect => defect.CauseCategoryCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
