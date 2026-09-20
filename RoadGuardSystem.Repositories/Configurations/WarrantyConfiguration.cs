using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Warranties;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("Warranties", table =>
        {
            table.HasCheckConstraint(
                "CK_Warranties_DateRange",
                "[WarrantyEndDate] >= [WarrantyStartDate]");
            table.HasCheckConstraint("CK_Warranties_RetainedValue_NonNegative", "[RetainedValue] IS NULL OR [RetainedValue] >= 0");
            table.HasCheckConstraint("CK_Warranties_Scope", "[Scope] IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_Warranties_Status", "[Status] IN (1, 2, 3, 4)");
            table.HasCheckConstraint(
                "CK_Warranties_ScopeRoadSection",
                "([Scope] <> 1 OR [RoadSectionId] IS NULL) AND ([Scope] <> 2 OR [RoadSectionId] IS NOT NULL)");
        });

        builder.HasKey(warranty => warranty.Id);
        builder.Property(warranty => warranty.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(warranty => warranty.ProjectId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(warranty => warranty.RoadSectionId)
            .HasColumnType("uniqueidentifier");
        builder.Property(warranty => warranty.HandoverDocumentId)
            .HasColumnType("uniqueidentifier");
        builder.Property(warranty => warranty.HandoverDate)
            .HasColumnType("date")
            .IsRequired();
        builder.Property(warranty => warranty.WarrantyStartDate)
            .HasColumnType("date")
            .IsRequired();
        builder.Property(warranty => warranty.WarrantyEndDate)
            .HasColumnType("date")
            .IsRequired();
        builder.Property(warranty => warranty.RetainedValue)
            .HasPrecision(19, 2);
        builder.Property(warranty => warranty.Scope)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(warranty => warranty.Terms)
            .HasColumnType("nvarchar(max)");
        builder.Property(warranty => warranty.SourceDocumentId)
            .HasColumnType("uniqueidentifier");
        builder.Property(warranty => warranty.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.HasIndex(warranty => warranty.ProjectId)
            .HasDatabaseName("IX_Warranties_ProjectId");
        builder.HasIndex(warranty => warranty.RoadSectionId)
            .HasDatabaseName("IX_Warranties_RoadSectionId");
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(warranty => warranty.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSection>()
            .WithMany()
            .HasForeignKey(warranty => warranty.RoadSectionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HandoverDocument>()
            .WithMany()
            .HasForeignKey(warranty => warranty.HandoverDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(warranty => warranty.SourceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
