using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.BusinessObjects.Identity;
namespace RoadGuardSystem.Repositories.Configurations;

internal static class DefectStatisticsConfiguration
{
    internal static void Configure(ModelBuilder model)
    {
        var source = model.Entity<DefectStatisticsSource>();
        source.ToTable("DefectStatisticsSources", table =>
        {
            table.HasTrigger("TR_DefectStatisticsSources_Immutable"); table.HasTrigger("TR_DefectStatisticsSources_Scope");
            table.HasCheckConstraint("CK_DefectStatisticsSources_Facts", "ISJSON([SegmentIdsJson])=1 AND ISJSON([QuantitiesJson])=1 AND ISJSON([SourceFactsJson])=1 AND [Provenance] COLLATE Latin1_General_100_BIN2 IN ('REAL_SOURCE','TEST_ONLY')");
            table.HasCheckConstraint("CK_DefectStatisticsSources_Bounds", "[From]>=0 AND [To]>[From] AND [OffsetTo]>[OffsetFrom]");
        });
        source.HasKey(row => row.Id); source.Property(row => row.Id).ValueGeneratedNever();
        source.Property(row => row.DefectVersion).HasMaxLength(100).IsUnicode(false); source.Property(row => row.ScopeHash).HasMaxLength(64).IsUnicode(false);
        source.Property(row => row.Provenance).HasMaxLength(20).IsUnicode(false); source.Property(row => row.LocationVersion).HasMaxLength(200);
        source.Property(row => row.Reason).HasMaxLength(2000);
        source.Property(row => row.From).HasPrecision(18, 3); source.Property(row => row.To).HasPrecision(18, 3);
        source.Property(row => row.OffsetFrom).HasPrecision(18, 3); source.Property(row => row.OffsetTo).HasPrecision(18, 3);
        source.HasIndex(row => row.SupersedesId).IsUnique().HasFilter("[SupersedesId] IS NOT NULL");
        source.HasIndex(row => new { row.ProjectId, row.SharedPartId });
        source.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        source.HasOne<Defect>().WithMany().HasForeignKey(row => row.DefectId).OnDelete(DeleteBehavior.Restrict);
        source.HasOne<RepairObligation>().WithMany().HasForeignKey(row => row.ObligationId).OnDelete(DeleteBehavior.Restrict);
        source.HasOne<RoadSection>().WithMany().HasForeignKey(row => row.RoadSectionId).OnDelete(DeleteBehavior.Restrict);
        source.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(row => row.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
        source.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.ActorId).OnDelete(DeleteBehavior.Restrict);
        source.HasOne<DefectStatisticsSource>().WithMany().HasForeignKey(row => row.SupersedesId).OnDelete(DeleteBehavior.Restrict);
    }
}
