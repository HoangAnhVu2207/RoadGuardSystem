using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Configurations;

internal static class Huy01Mapping
{
    internal static void Snapshot<T>(EntityTypeBuilder<T> builder, string field, string column) where T : class
    {
        var p = builder.Property<List<Guid>>(field).HasColumnName(column).HasColumnType("nvarchar(max)")
            .HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)!);
        p.Metadata.SetValueComparer(new ValueComparer<List<Guid>>(
            (a, other) => a!.SequenceEqual(other!), v => v.Aggregate(0, (h, id) => HashCode.Combine(h, id)), v => v.ToList()));
    }
    internal static void Head<T>(EntityTypeBuilder<T> builder) where T : class
    {
        builder.Property<long>("Revision").HasDefaultValue(0L);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

public sealed class HuyReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports", t => t.UseSqlOutputClause(false)); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.ReporterUserId });
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        Huy01Mapping.Head(builder);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReporterUserId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(x => x.OriginalEvidence, e =>
        {
            e.ToTable("ReportOriginalEvidence", t => t.UseSqlOutputClause(false).HasCheckConstraint("CK_ReportOriginalEvidence_Original", "[SupplementId] IS NULL AND [VerificationState] = 1"));
            e.WithOwner().HasForeignKey(x => x.ReportId); Evidence(e);
        });
        builder.Navigation(x => x.OriginalEvidence).HasField("_originalEvidence").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Supplements).WithOne().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Supplements).HasField("_supplements").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
    internal static void Evidence<TOwner>(OwnedNavigationBuilder<TOwner, ReportEvidence> e) where TOwner : class
    {
        e.WithOwner().Metadata.DeleteBehavior = DeleteBehavior.Restrict;
        e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedNever();
        e.HasIndex(x => new { x.Id, x.ReportId }).IsUnique();
        e.Property(x => x.FileVersion).HasMaxLength(200).IsRequired();
        e.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Report>().WithMany().HasForeignKey(x => new { x.ReportId, x.OwnerUserId })
            .HasPrincipalKey(x => new { x.Id, x.ReporterUserId }).OnDelete(DeleteBehavior.Restrict);
        e.OwnsOne(x => x.CaptureMetadata, m =>
        {
            m.Property(x => x.Latitude).HasPrecision(10, 7);
            m.Property(x => x.Longitude).HasPrecision(10, 7);
            m.Property(x => x.AccuracyMeters).HasPrecision(18, 3);
        });
    }
}

public sealed class HuySupplementConfiguration : IEntityTypeConfiguration<ReportSupplement>
{
    public void Configure(EntityTypeBuilder<ReportSupplement> builder)
    {
        builder.ToTable("ReportSupplements", t => t.UseSqlOutputClause(false)); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.ReportId });
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.OwnsMany(x => x.Evidence, e =>
        {
            e.ToTable("ReportSupplementEvidence", t => t.UseSqlOutputClause(false).HasCheckConstraint("CK_ReportSupplementEvidence_Supplement", "[SupplementId] IS NOT NULL AND [VerificationState] = 1"));
            e.WithOwner().HasForeignKey(x => new { x.SupplementId, x.ReportId }).HasPrincipalKey(x => new { x.Id, x.ReportId });
            HuyReportConfiguration.Evidence(e);
        });
        builder.Navigation(x => x.Evidence).HasField("_evidence").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class HuyCaseConfiguration : IEntityTypeConfiguration<IncidentCase>
{
    public void Configure(EntityTypeBuilder<IncidentCase> builder)
    {
        builder.ToTable("IncidentCases", t => t.UseSqlOutputClause(false).HasCheckConstraint("CK_IncidentCases_GeometryPair", "([GeometryRouteVersionId] IS NULL AND [GeometrySegmentSetId] IS NULL) OR ([GeometryRouteVersionId] IS NOT NULL AND [GeometrySegmentSetId] IS NOT NULL)"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); Huy01Mapping.Head(builder);
        builder.Property(x => x.TriageReason).HasMaxLength(1000);
        builder.Property<Guid?>("GeometryRouteVersionId"); builder.Property<Guid?>("GeometrySegmentSetId");
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey("GeometrySegmentSetId", "GeometryRouteVersionId")
            .HasPrincipalKey(x => new { x.Id, x.RoadSectionVersionId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IncidentCase>().WithMany().HasForeignKey(x => x.LinkedTargetCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(x => x.ActiveReportIds); Huy01Mapping.Snapshot(builder, "_activeReportIds", "ActiveReportIdsJson");
        // The same history object belongs to both source and target: a single navigation
        // cannot hydrate it safely. Huy queries FromCaseId OR ToCaseId explicitly.
        builder.Ignore(x => x.LinkHistory);
        builder.HasMany(x => x.Conclusions).WithOne().HasForeignKey("CaseId").OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Conclusions).HasField("_conclusions").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Publications).WithOne().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Publications).HasField("_publications").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class HuyConclusionConfiguration : IEntityTypeConfiguration<CaseConclusion>
{
    public void Configure(EntityTypeBuilder<CaseConclusion> builder)
    {
        builder.ToTable("CaseConclusions", t => t.UseSqlOutputClause(false)); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); builder.Property<Guid>("CaseId");
        builder.Ignore(x => x.DefectIds); builder.Ignore(x => x.EvidenceIds);
        Huy01Mapping.Snapshot(builder, "_defectIds", "DefectIdsJson"); Huy01Mapping.Snapshot(builder, "_evidenceIds", "EvidenceIdsJson");
    }
}

public sealed class HuyPublicationConfiguration : IEntityTypeConfiguration<CasePublication>
{
    public void Configure(EntityTypeBuilder<CasePublication> builder)
    {
        builder.ToTable("CasePublications", t => t.UseSqlOutputClause(false)); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Summary).HasMaxLength(1000).IsRequired();
        builder.Ignore(x => x.RecipientReportIds); builder.Ignore(x => x.DefectIds); builder.Ignore(x => x.EvidenceIds);
        Huy01Mapping.Snapshot(builder, "_recipientReportIds", "RecipientReportIdsJson");
        Huy01Mapping.Snapshot(builder, "_defectIds", "DefectIdsJson"); Huy01Mapping.Snapshot(builder, "_evidenceIds", "EvidenceIdsJson");
    }
}

public sealed class HuyLinkHistoryConfiguration : IEntityTypeConfiguration<CaseReportLinkHistory>
{
    public void Configure(EntityTypeBuilder<CaseReportLinkHistory> builder)
    {
        builder.ToTable("CaseReportLinkHistory", t => t.UseSqlOutputClause(false)); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); builder.Ignore(x => x.ReportIds);
        Huy01Mapping.Snapshot(builder, "_reportIds", "ReportIdsJson");
        builder.HasOne<IncidentCase>().WithMany().HasForeignKey(x => x.FromCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IncidentCase>().WithMany().HasForeignKey(x => x.ToCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyDecisionConfiguration : IEntityTypeConfiguration<CandidateDecision>
{
    public void Configure(EntityTypeBuilder<CandidateDecision> builder)
    {
        builder.ToTable("SourceDecisions", t => t.UseSqlOutputClause(false).HasCheckConstraint("CK_SourceDecisions_TypedSource",
            "[SourceKind] = [Source_Kind] AND [SourceId] = [Source_Id] AND (([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [ReportSourceId] IS NOT NULL AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [AIDetectionSourceId] IS NOT NULL AND [ReportSourceId] IS NULL))"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property<CandidateSourceKind>("SourceKind"); builder.Property<Guid>("SourceId");
        builder.Property<Guid?>("ReportSourceId"); builder.Property<Guid?>("AIDetectionSourceId");
        builder.HasAlternateKey("Id", "SourceKind", "SourceId", nameof(CandidateDecision.ProjectId));
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(x => x.GeometryVersion).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetDefectVersion).HasMaxLength(200);
        builder.Property(x => x.ExpectedPreviousDecisionVersion).HasMaxLength(200);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.OwnsOne(x => x.Source, s =>
        {
            s.Property(x => x.Kind).HasColumnName("Source_Kind");
            s.Property(x => x.Id).HasColumnName("Source_Id");
            s.Property(x => x.SourceVersion).HasColumnName("Source_Version").HasMaxLength(200).IsRequired();
        });
        builder.Navigation(x => x.Source).IsRequired();
        builder.OwnsOne(x => x.Classification, c =>
        {
            c.Property(x => x.DefectTypeCode).HasMaxLength(80).IsUnicode(false);
            c.Property(x => x.CauseCategoryCode).HasMaxLength(80).IsUnicode(false);
            c.HasOne<DefectType>().WithMany().HasForeignKey(x => x.DefectTypeCode).OnDelete(DeleteBehavior.Restrict);
            c.HasOne<CauseCategory>().WithMany().HasForeignKey(x => x.CauseCategoryCode).OnDelete(DeleteBehavior.Restrict);
            c.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(x => x.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
            c.HasOne<RoadSegment>().WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.HasOne<Report>().WithMany().HasForeignKey("ReportSourceId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIDetection>().WithMany().HasForeignKey("AIDetectionSourceId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.TargetDefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CandidateDecision>().WithMany().HasForeignKey(x => x.SupersedesDecisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyLinkConfiguration : IEntityTypeConfiguration<HuyCaseReportLink>
{
    public void Configure(EntityTypeBuilder<HuyCaseReportLink> builder)
    {
        builder.ToTable("CaseReportLinks", t => t.UseSqlOutputClause(false).HasCheckConstraint("CK_CaseReportLinks_Times", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.ReportId).IsUnique().HasFilter("[EndedAt] IS NULL");
        builder.HasIndex(x => new { x.CaseId, x.ReportId });
        builder.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IncidentCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyRecipientConfiguration : IEntityTypeConfiguration<HuyPublicationRecipient>
{
    public void Configure(EntityTypeBuilder<HuyPublicationRecipient> builder)
    {
        builder.ToTable("CasePublicationRecipients", t => t.UseSqlOutputClause(false)); builder.HasKey(x => new { x.PublicationId, x.ReportId });
        builder.HasOne<CasePublication>().WithMany().HasForeignKey(x => x.PublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyPublicationEvidenceConfiguration : IEntityTypeConfiguration<HuyPublicationEvidence>
{
    public void Configure(EntityTypeBuilder<HuyPublicationEvidence> builder)
    {
        builder.ToTable("CasePublicationEvidence", t => t.UseSqlOutputClause(false).HasCheckConstraint("CK_CasePublicationEvidence_TypedEvidence",
            "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)"));
        builder.HasKey(x => new { x.PublicationId, x.RecipientReportId, x.EvidenceId });
        builder.HasOne<HuyPublicationRecipient>().WithMany().HasForeignKey(x => new { x.PublicationId, x.RecipientReportId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Report>().WithMany().HasForeignKey(x => x.SourceReportId).OnDelete(DeleteBehavior.Restrict);
        // Typed composite evidence FKs target owned types in separate tables. The
        // shared writer adds these relational constraints in the migration.
    }
}

public sealed class HuySourceHeadConfiguration : IEntityTypeConfiguration<HuyCandidateSourceHead>
{
    public void Configure(EntityTypeBuilder<HuyCandidateSourceHead> builder)
    {
        builder.ToTable("CandidateSourceHeads", t => t.UseSqlOutputClause(false)); builder.HasKey(x => new { x.SourceKind, x.SourceId });
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<CandidateDecision>().WithMany().HasForeignKey(x => new { x.DecisionId, x.SourceKind, x.SourceId, x.ProjectId })
            .HasPrincipalKey("Id", "SourceKind", "SourceId", nameof(CandidateDecision.ProjectId)).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyDefectSourceLinkConfiguration : IEntityTypeConfiguration<HuyDefectSourceLink>
{
    public void Configure(EntityTypeBuilder<HuyDefectSourceLink> builder)
    {
        builder.ToTable("DefectSourceLinks", table => table.HasCheckConstraint("CK_DefectSourceLinks_TypedSource",
            "([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR " +
            "([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)"));
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.Property(link => link.RowVersion).IsRowVersion();
        builder.HasIndex(link => new { link.SourceKind, link.SourceId })
            .IsUnique().HasFilter("[EndedAt] IS NULL");
        builder.HasIndex(link => link.DecisionId).IsUnique();
        builder.HasIndex(link => link.DefectId);
        builder.HasOne<Report>().WithMany().HasForeignKey(link => link.ReportSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIDetection>().WithMany().HasForeignKey(link => link.AIDetectionSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(link => link.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(link => link.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CandidateDecision>().WithMany()
            .HasForeignKey(link => new { link.DecisionId, link.SourceKind, link.SourceId, link.ProjectId })
            .HasPrincipalKey("Id", "SourceKind", "SourceId", nameof(CandidateDecision.ProjectId))
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyConclusionDefectConfiguration : IEntityTypeConfiguration<HuyConclusionDefect>
{
    public void Configure(EntityTypeBuilder<HuyConclusionDefect> builder)
    {
        builder.ToTable("CaseConclusionDefects", t => t.UseSqlOutputClause(false)); builder.HasKey(x => new { x.ConclusionId, x.DefectId });
        builder.HasOne<CaseConclusion>().WithMany().HasForeignKey(x => x.ConclusionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyPublicationDefectConfiguration : IEntityTypeConfiguration<HuyPublicationDefect>
{
    public void Configure(EntityTypeBuilder<HuyPublicationDefect> builder)
    {
        builder.ToTable("CasePublicationDefects", t => t.UseSqlOutputClause(false)); builder.HasKey(x => new { x.PublicationId, x.DefectId });
        builder.HasOne<CasePublication>().WithMany().HasForeignKey(x => x.PublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(x => x.DefectId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyConclusionEvidenceConfiguration : IEntityTypeConfiguration<HuyConclusionEvidence>
{
    public void Configure(EntityTypeBuilder<HuyConclusionEvidence> builder)
    {
        builder.ToTable("CaseConclusionEvidence", t => t.UseSqlOutputClause(false).HasCheckConstraint("CK_CaseConclusionEvidence_TypedEvidence",
            "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)"));
        builder.HasKey(x => new { x.ConclusionId, x.EvidenceId });
        builder.HasOne<CaseConclusion>().WithMany().HasForeignKey(x => x.ConclusionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Report>().WithMany().HasForeignKey(x => x.SourceReportId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HuyLinkHistoryReportConfiguration : IEntityTypeConfiguration<HuyLinkHistoryReport>
{
    public void Configure(EntityTypeBuilder<HuyLinkHistoryReport> builder)
    {
        builder.ToTable("CaseReportLinkHistoryReports", t => t.UseSqlOutputClause(false)); builder.HasKey(x => new { x.HistoryId, x.ReportId });
        builder.HasOne<CaseReportLinkHistory>().WithMany().HasForeignKey(x => x.HistoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
    }
}
