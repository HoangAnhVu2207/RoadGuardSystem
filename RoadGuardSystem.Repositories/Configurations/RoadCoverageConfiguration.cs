using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

internal static class RoadCoverageConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var mapping = model.Entity<RoadCoverageMapping>();
        mapping.ToTable("RoadCoverageMappings", table =>
        {
            table.HasTrigger("TR_RoadCoverageMappings_Immutable");
            table.HasTrigger("TR_RoadCoverageMappings_Scope");
            table.HasCheckConstraint("CK_RoadCoverageMappings_Source", "[SourceKind] COLLATE Latin1_General_100_BIN2 IN ('WARRANTY','MAINTENANCE_BASIS') AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY') AND ISJSON([SourceFactsJson])=1");
            table.HasCheckConstraint("CK_RoadCoverageMappings_Bounds", "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom] AND [ApplicableToUtc]>[ApplicableFromUtc]");
        });
        mapping.HasKey(row => row.Id); mapping.Property(row => row.Id).ValueGeneratedNever();
        mapping.Property(row => row.LocationVersion).HasMaxLength(200).IsRequired();
        mapping.Property(row => row.SourceKind).HasMaxLength(30).IsUnicode(false).IsRequired();
        mapping.Property(row => row.Provenance).HasMaxLength(20).IsUnicode(false).IsRequired();
        mapping.Property(row => row.SourceVersion).HasMaxLength(64).IsUnicode(false).IsRequired();
        mapping.Property(row => row.HandoverVersion).HasMaxLength(64).IsUnicode(false).IsRequired();
        mapping.Property(row => row.Reason).HasMaxLength(2000).IsRequired();
        mapping.Property(row => row.SourceFactsJson).IsRequired();
        mapping.Property(row => row.From).HasPrecision(18, 3); mapping.Property(row => row.To).HasPrecision(18, 3);
        mapping.Property(row => row.OffsetFrom).HasPrecision(18, 3); mapping.Property(row => row.OffsetTo).HasPrecision(18, 3);
        mapping.HasIndex(row => row.SupersedesId).IsUnique().HasFilter("[SupersedesId] IS NOT NULL");
        mapping.HasIndex(row => new { row.ProjectId, row.RoadSectionId, row.LocationVersion });
        mapping.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne<RoadSection>().WithMany().HasForeignKey(row => row.RoadSectionId).OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne<RepairObligation>().WithMany().HasForeignKey(row => row.ScopeObligationId).OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne<HandoverDocument>().WithMany().HasForeignKey(row => row.HandoverDocumentId).OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne<StoredFile>().WithMany().HasForeignKey(row => row.HandoverFileId).OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne<StoredFile>().WithMany().HasForeignKey(row => row.CoverageFileId).OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.ActorId).OnDelete(DeleteBehavior.Restrict);
        mapping.HasOne<RoadCoverageMapping>().WithMany().HasForeignKey(row => row.SupersedesId).OnDelete(DeleteBehavior.Restrict);
    }
}
