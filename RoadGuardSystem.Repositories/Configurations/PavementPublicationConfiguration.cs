using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Files;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class PavementPublicationConfiguration : IEntityTypeConfiguration<PavementLayoutRevision>,
    IEntityTypeConfiguration<GeometryMapPublication>, IEntityTypeConfiguration<GeometryLocationImpact>,
    IEntityTypeConfiguration<GeometryLocationImpactDecision>, IEntityTypeConfiguration<PavementSourceFileReference>
{
    public void Configure(EntityTypeBuilder<PavementLayoutRevision> builder)
    {
        builder.ToTable("PavementLayoutRevisions", t =>
        {
            t.HasTrigger("TR_PavementLayoutRevisions_Immutable");
            t.HasTrigger("TR_PavementLayoutRevisions_Scope");
            t.HasCheckConstraint("CK_PavementLayoutRevisions_Kind", "[Kind] IN ('PLANNED','AS_BUILT') AND (([Kind]='PLANNED' AND [SourcePlanId] IS NULL) OR ([Kind]='AS_BUILT' AND [SourcePlanId] IS NOT NULL))");
            t.HasCheckConstraint("CK_PavementLayoutRevisions_Json", "ISJSON([DefinitionJson])=1 AND ISJSON([SnapshotJson])=1");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasMaxLength(16); builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DefinitionJson).IsRequired(); builder.Property(x => x.SnapshotJson).IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey(x => x.SegmentSetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PavementLayoutRevision>().WithMany().HasForeignKey(x => x.SourcePlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CrsProfileRevision>().WithMany().HasForeignKey(x => new { x.CrsProfileRevisionId, x.ProjectId })
            .HasPrincipalKey(x => new { x.Id, x.ProjectId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RouteVersionId, x.CreatedAt, x.Id });
    }
    public void Configure(EntityTypeBuilder<GeometryMapPublication> builder)
    {
        builder.ToTable("GeometryMapPublications", t =>
        {
            t.HasTrigger("TR_GeometryMapPublications_Immutable");
            t.HasTrigger("TR_GeometryMapPublications_Scope");
            t.HasCheckConstraint("CK_GeometryMapPublications_Mode", "[PublicationMode] IN ('SAMPLE','OFFICIAL')");
            t.HasCheckConstraint("CK_GeometryMapPublications_Json", "ISJSON([SnapshotJson])=1");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PublicationMode).HasMaxLength(16); builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey(x => x.SegmentSetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PavementLayoutRevision>().WithMany().HasForeignKey(x => x.LayoutRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PublishedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CrsProfileRevision>().WithMany().HasForeignKey(x => new { x.CrsProfileRevisionId, x.ProjectId })
            .HasPrincipalKey(x => new { x.Id, x.ProjectId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProjectId, x.PublishedAt, x.Id });
    }
    public void Configure(EntityTypeBuilder<GeometryLocationImpact> builder)
    {
        builder.ToTable("GeometryLocationImpacts", t =>
        {
            t.HasTrigger("TR_GeometryLocationImpacts_Immutable");
            t.HasTrigger("TR_GeometryLocationImpacts_Scope");
            t.HasCheckConstraint("CK_GeometryLocationImpacts_Json", "ISJSON([AffectedReferencesJson])=1");
            t.HasCheckConstraint("CK_GeometryLocationImpacts_Versions", "[PreviousRouteVersionId]<>[NewRouteVersionId]");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AffectedReferencesJson).IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(x => x.PreviousRouteVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(x => x.NewRouteVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PreviousRouteVersionId, x.NewRouteVersionId }).IsUnique();
    }
    public void Configure(EntityTypeBuilder<GeometryLocationImpactDecision> builder)
    {
        builder.ToTable("GeometryLocationImpactDecisions", t =>
        {
            t.HasTrigger("TR_GeometryLocationImpactDecisions_Immutable");
            t.HasCheckConstraint("CK_GeometryLocationImpactDecisions_Action", "[Action] IN ('VERIFY','CONTINUE','STOP','REASSIGN')");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Action).HasMaxLength(16); builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.HasOne<GeometryLocationImpact>().WithMany().HasForeignKey(x => x.ImpactId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ImpactId, x.TaskId }).IsUnique();
        // TaskId deliberately has no guessed FK: H3 integrates actual FIELD task admission.
    }
    public void Configure(EntityTypeBuilder<PavementSourceFileReference> builder)
    {
        builder.ToTable("PavementSourceFileReferences", t =>
        {
            t.HasTrigger("TR_PavementSourceFileReferences_Immutable");
            t.HasTrigger("TR_PavementSourceFileReferences_Scope");
            t.HasCheckConstraint("CK_PavementSourceFileReferences_Facts", "LEN([ContentChecksum])=64 AND ISJSON([CaptureFactsJson])=1");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContentChecksum).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CaptureFactsJson).IsRequired();
        builder.HasOne<PavementLayoutRevision>().WithMany().HasForeignKey(x => x.LayoutRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LayoutRevisionId, x.FileId }).IsUnique();
    }
}
