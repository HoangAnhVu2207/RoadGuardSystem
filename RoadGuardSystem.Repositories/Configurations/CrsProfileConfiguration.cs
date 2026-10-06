using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class CrsProfileConfiguration : IEntityTypeConfiguration<CrsProfileRevision>, IEntityTypeConfiguration<RoadRouteSystem>, IEntityTypeConfiguration<NativeRouteVersionFacts>
{
    public void Configure(EntityTypeBuilder<CrsProfileRevision> builder)
    {
        builder.ToTable("CrsProfileRevisions", t => { t.HasTrigger("TR_CrsProfileRevisions_Immutable"); t.HasCheckConstraint("CK_CrsProfileRevision", "[Revision]>0 AND [SourceSrid]>=0 AND [Status] IN ('CANDIDATE','VERIFIED') AND ISJSON([PayloadJson])=1"); });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.Code).HasMaxLength(80); builder.Property(x => x.Status).HasMaxLength(16);
        builder.HasAlternateKey(x => new { x.Id, x.ProjectId }); builder.HasIndex(x => new { x.ProjectId, x.Code, x.Revision }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<RoadRouteSystem> builder)
    {
        builder.ToTable("RoadRouteSystems"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(80); builder.Property(x => x.Name).HasMaxLength(255);
        builder.HasAlternateKey(x => new { x.Id, x.ProjectId }); builder.HasIndex(x => new { x.ProjectId, x.Code }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<NativeRouteVersionFacts> builder)
    {
        builder.ToTable("NativeRouteVersionFacts", t => { t.HasTrigger("TR_NativeRouteVersionFacts_Immutable"); t.HasTrigger("TR_NativeRouteVersionFacts_Scope"); t.HasCheckConstraint("CK_NativeRouteFacts", "[CanonicalLengthMeters]>0 AND ([DeclaredLengthMeters] IS NULL OR [DeclaredLengthMeters]>0) AND (([RouteKind]='MAIN' AND [ParentRouteVersionId] IS NULL AND [JunctionOffsetMeters] IS NULL) OR ([RouteKind]='BRANCH' AND [ParentRouteVersionId] IS NOT NULL AND [JunctionOffsetMeters] IS NOT NULL AND [JunctionOffsetMeters]>=0)) AND ([CalibrationJson] IS NULL OR ISJSON([CalibrationJson])=1)"); });
        builder.HasKey(x => x.RoadSectionVersionId); builder.Property(x => x.RoadSectionVersionId).ValueGeneratedNever(); builder.Property(x => x.RouteKind).HasMaxLength(16);
        builder.HasOne<RoadSectionVersion>().WithOne().HasForeignKey<NativeRouteVersionFacts>(x => x.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(x => x.ParentRouteVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CrsProfileRevision>().WithMany().HasForeignKey(x => new { x.CrsProfileRevisionId, x.ProjectId }).HasPrincipalKey(x => new { x.Id, x.ProjectId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadRouteSystem>().WithMany().HasForeignKey(x => new { x.RouteSystemId, x.ProjectId }).HasPrincipalKey(x => new { x.Id, x.ProjectId }).OnDelete(DeleteBehavior.Restrict);
    }
}
