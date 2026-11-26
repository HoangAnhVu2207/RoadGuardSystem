using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ReportMatchingLocationConfiguration : IEntityTypeConfiguration<ReportMatchingLocation>
{
    public void Configure(EntityTypeBuilder<ReportMatchingLocation> builder)
    {
        builder.ToTable("ReportMatchingLocations", table => table.HasTrigger("TR_ReportMatchingLocations_Immutable"));
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.HasIndex(row => new { row.ReportId, row.Sequence }).IsUnique();
        builder.Property(row => row.Reason).HasMaxLength(2000);
        builder.Property(row => row.GeometryVersion).HasMaxLength(256);
        builder.Property(row => row.ReportVersion).HasMaxLength(32);
        builder.Property(row => row.CaseVersion).HasMaxLength(32);
        builder.Property(row => row.Latitude).HasPrecision(18, 10);
        builder.Property(row => row.Longitude).HasPrecision(18, 10);
        builder.Property(row => row.AccuracyMeters).HasPrecision(18, 3);
        builder.HasOne<Report>().WithMany().HasForeignKey(row => row.ReportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IncidentCase>().WithMany().HasForeignKey(row => row.CaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(row => row.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey(row => row.SegmentSetId).OnDelete(DeleteBehavior.Restrict);
    }
}
