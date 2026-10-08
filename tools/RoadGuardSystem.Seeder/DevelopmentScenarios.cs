using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Seeding;
using RoadGuardSystem.Repositories.Implementations.Surveys;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Projects;
using RoadGuardSystem.Services.Surveys;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Options;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Services.Implementations.Files;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Seeder;

public sealed record SeedTableCoverage(string Table, long Count, string Producer, string? EmptyReason,
    string Group = "", int DependencyOrder = 0, string[]? Prerequisites = null, string[]? Source = null);
public sealed record DevelopmentSeedManifest(string Evidence, Guid ProjectId, DateTimeOffset ReferenceTime,
    IReadOnlyList<object> Accounts, IReadOnlyList<SeedTableCoverage> Tables, IReadOnlyList<string> Gaps,
    IReadOnlyList<object>? Scenarios = null);
internal sealed record CoverageCatalogRow(string Table, string Group, int DependencyOrder, string[] Prerequisites,
    string Producer, string EmptyReason, string Scenario, string[] Source);

/// <summary>Supported producers for the existing PostmanScenarioSeedStep, composed outside Repositories.</summary>
public static class DevelopmentScenarios
{
    public const string ProjectCode = "RG-CI-01-SYNTHETIC";
    private const string RoadCode = "RG-CI-01-SAMPLE-ROAD";
    private static readonly Guid OperationId = Guid.Parse("8305e20c-0785-4012-a490-baf002852c01");
    private static readonly JsonSerializerOptions ReadJson = new() { PropertyNameCaseInsensitive = true };

    public static Task<DevelopmentSeedManifest> SeedAsync(RoadGuardDbContext db, CancellationToken ct = default)
        => SeedAsync(db, null, ct);

    public static async Task<DevelopmentSeedManifest> SeedAsync(RoadGuardDbContext db, string? storageConfigPath, CancellationToken ct = default)
    {
        var gaps = new List<string>();
        var clock = TimeProvider.System;
        var receipts = new IdempotencyOperationService(db);
        var project = await db.Projects.SingleOrDefaultAsync(x => x.ProjectCode == ProjectCode, ct);
        if (project is null)
        {
            var now = clock.GetUtcNow();
            var day = DateOnly.FromDateTime(now.UtcDateTime);
            var create = await new ProjectCreationService(new ProjectCreationPersistenceService(db, receipts))
                .CreateAsync(PostmanUserSeedStep.SupervisorUserId, UserRoleCode.Supervisor,
                    new(ProjectCode, "Synthetic development project", "RG-CI-01 synthetic dev/test; no construction authority.",
                        32648, day, day.AddYears(1), PostmanUserSeedStep.ProjectManagerUserId,
                        "RG-CI-01-SYNTHETIC-HANDOVER", day, null, "Synthetic demonstration only", OperationId, null,
                        day.AddYears(1), IdempotencyKey: "rg-ci-01/project"), ct);
            if (create.Project is null) throw new InvalidOperationException($"Development project producer rejected: {create.Status}.");
            db.ChangeTracker.Clear();
            project = await db.Projects.SingleAsync(x => x.Id == create.Project.ProjectId, ct);
        }
        // Existing project identity is never reassigned; mutable details and lifecycle state are preserved.
        var reference = project.CreatedAt;
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), clock);
        var geometry = new GeometryWorkflowService(new GeometryWorkflowPersistenceService(db, receipts, clock), guard);
        if (project.Status == ProjectStatus.Active)
        {
            await EnsureMembershipAsync(db, project.Id, PostmanUserSeedStep.OperatorUserId, UserRoleCode.DroneOperator, reference, ct);
            await EnsureMembershipAsync(db, project.Id, PostmanUserSeedStep.RepairCrewUserId, UserRoleCode.RepairCrew, reference, ct);
            if (!await db.DefectTypes.AnyAsync(x => x.Code == PostmanScenarioSeedStep.DefectTypeCode, ct))
            {
                db.DefectTypes.Add(DefectType.Create(PostmanScenarioSeedStep.DefectTypeCode, "Synthetic pothole", "Dev/test catalog; not verified defect."));
                await db.SaveChangesAsync(ct);
            }
            async Task<GeometryWorkflowResult> Run(string action, object? input = null, Guid? draft = null,
                Guid? section = null, Guid? route = null, Guid? set = null, string? version = null)
            {
                var actor = action == "confirm" ? PostmanUserSeedStep.SupervisorUserId : PostmanUserSeedStep.ProjectManagerUserId;
                var role = action == "confirm" ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager;
                var result = await geometry.ExecuteAsync(role, new(actor, project.Id, action, section, draft, route, set,
                    input, "rg-ci-01/geometry/" + action, version), ct);
                if (result.Status >= 400) throw new InvalidOperationException($"Geometry {action}: {result.Status}/{result.Code}");
                return result;
            }
            try
            {
                var profile = await db.Set<CrsProfileRevision>().SingleOrDefaultAsync(x => x.ProjectId == project.Id && x.Code == "RG-CI-01-SAMPLE", ct);
                if (profile is null)
                {
                    await Run("profile-create", new CrsProfileInput("RG-CI-01-SAMPLE", 0, "synthetic local datum", "synthetic local plane",
                        "EN", 1, "RG-CI-01 synthetic fixture", new string('a', 64),
                        new("SAMPLE_AFFINE_TO_WGS84", "synthetic", "NATIVE_TO_WGS84", [0.00001, 0, 106, 0, 0.00001, 10], "synthetic fixture"), SampleOnly: true));
                    profile = await db.Set<CrsProfileRevision>().SingleAsync(x => x.ProjectId == project.Id && x.Code == "RG-CI-01-SAMPLE", ct);
                }
                var system = await db.Set<RoadRouteSystem>().SingleOrDefaultAsync(x => x.ProjectId == project.Id && x.Code == "RG-CI-01-SAMPLE", ct);
                if (system is null)
                {
                    await Run("system-create", new RouteSystemInput("RG-CI-01-SAMPLE", "Synthetic local route"));
                    system = await db.Set<RoadRouteSystem>().SingleAsync(x => x.ProjectId == project.Id && x.Code == "RG-CI-01-SAMPLE", ct);
                }
                var draft = await db.Set<RoadGeometryDraft>().SingleOrDefaultAsync(x => x.ProjectId == project.Id && x.RoadCode == RoadCode, ct);
                var newDraft = draft is null;
                if (newDraft)
                {
                    var native = new NativeAlignmentInput(profile.Id, [new("LINE", new(0, 0), new(200, 0))]);
                    await Run("draft-create", new GeometryDraftInput("NATIVE_ALIGNMENT", 0, 0, "Synthetic dev/test sample",
                        null, null, null, null, [new(0, 200, 6)], 10, RoadCode: RoadCode, RoadName: "Synthetic sample road",
                        NativeAlignment: native, RouteSystemId: system.Id, RouteKind: "MAIN", TessellationToleranceMeters: 0.1));
                    draft = await db.Set<RoadGeometryDraft>().SingleAsync(x => x.ProjectId == project.Id && x.RoadCode == RoadCode, ct);
                }
                // Only advance a newly created graph. A user-edited/progressed graph remains untouched on rerun.
                if (newDraft)
                {
                    await Run("draft-preview", draft: draft!.Id);
                    await Run("confirm", new GeometryConfirmInput(null, reference, "Synthetic sample confirmation; no construction authority"),
                        draft.Id, version: Convert.ToBase64String(draft.RowVersion));
                }
                var section = await db.RoadSections.SingleOrDefaultAsync(x => x.ProjectId == project.Id && x.Code == RoadCode, ct);
                var route = section is null ? null : await db.RoadSectionVersions.SingleOrDefaultAsync(x => x.RoadSectionId == section.Id && x.IsCurrent, ct);
                if (route is not null)
                {
                    var set = await db.RoadSegmentSets.SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id, ct);
                    if (set is null)
                    {
                        await Run("set-create", new SegmentDefinition(100), section: section!.Id, route: route.Id);
                        set = await db.RoadSegmentSets.SingleAsync(x => x.RoadSectionVersionId == route.Id, ct);
                        await Run("publish", new SegmentPublishInput(null, "Synthetic dev/test sample publication"), section: section.Id,
                            route: route.Id, set: set.Id, version: Convert.ToBase64String(set.RowVersion));
                    }
                    if (set.Status == "PUBLISHED")
                    {
                        await SeedSurveyAsync(db, guard, receipts, project.Id, route.Id, set.Id, reference, ct);
                        if (!await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM PavementLayoutRevisions WHERE ProjectId={0}", project.Id).AnyAsync(x => x > 0, ct))
                        {
                            var pavement = await new PavementWorkflowService(new PavementWorkflowRepository(db, receipts, clock), guard)
                                .ExecuteAsync(UserRoleCode.ProjectManager, new(PostmanUserSeedStep.ProjectManagerUserId,
                                    project.Id, "plan-create", route.Id, set.Id, Input: new PavementPlanCreateInput(
                                        new(3, 5, [new(0, 200, 6)]), 0.1), Key: "rg-ci-01/pavement/plan"), ct);
                            if (pavement.Status >= 400) gaps.Add($"Pavement plan producer: {pavement.Status}/{pavement.Code}");
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                db.ChangeTracker.Clear();
                gaps.Add("Geometry/survey supported producer: " + ex.Message);
            }
        }
        else gaps.Add("Existing project is not active; seed preserves user lifecycle state and skips new transitions.");
        if (project.Status == ProjectStatus.Active)
        {
            if (!await db.Set<RoadGuardSystem.BusinessObjects.Repairs.RepairPolicyDraft>().AnyAsync(x => x.ProjectId == project.Id, ct))
            {
                var policy = await new RoadGuardSystem.Services.Implementations.Repairs.RepairPolicyService(
                    new RoadGuardSystem.Repositories.Implementations.Repairs.RepairPolicyRepository(db, receipts, clock))
                    .ExecuteAsync(PostmanUserSeedStep.ProjectManagerUserId, UserRoleCode.ProjectManager, project.Id, "create", null,
                        new(PostmanScenarioSeedStep.DefectTypeCode, "synthetic-v1", [new("LENGTH", "m", 0.01m, 1m)],
                            ["Synthetic demonstration; PM must approve real use"], "Synthetic dev/test policy draft"),
                        null, "rg-ci-01/policy/draft", null, ct);
                if (policy.Status >= 400) gaps.Add($"Policy draft producer: {policy.Status}/{policy.Code}");
                else if (policy.Value is { } draftPolicy)
                {
                    var publishedPolicy = await new RoadGuardSystem.Services.Implementations.Repairs.RepairPolicyService(
                        new RoadGuardSystem.Repositories.Implementations.Repairs.RepairPolicyRepository(db, receipts, clock))
                        .ExecuteAsync(PostmanUserSeedStep.ProjectManagerUserId, UserRoleCode.ProjectManager, project.Id, "publish",
                            draftPolicy.Id, null, "Synthetic dev/test policy; no construction authority", "rg-ci-01/policy/publish",
                            '"' + draftPolicy.Version + '"', ct);
                    if (publishedPolicy.Status >= 400) gaps.Add($"Policy publication producer: {publishedPolicy.Status}/{publishedPolicy.Code}");
                }
            }
            var device = Guid.Parse("8305e20c-0785-4012-a490-baf002852c02");
            if (!await db.Set<RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration>().AnyAsync(x => x.ProjectId == project.Id && x.DeviceId == device, ct))
            {
                using var encryption = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                using var signing = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var offline = await new RoadGuardSystem.Services.Offline.OfflineWorkflowService(
                    new RoadGuardSystem.Repositories.Implementations.Offline.OfflineWorkflowRepository(db, clock), guard)
                    .ExecuteAsync(PostmanUserSeedStep.RepairCrewUserId, UserRoleCode.RepairCrew, project.Id, "device-register",
                        new RoadGuardSystem.DTOs.Offline.OfflineDeviceRegisterInput(device,
                            Convert.ToBase64String(encryption.ExportSubjectPublicKeyInfo()),
                            Convert.ToBase64String(signing.ExportSubjectPublicKeyInfo())), null, "rg-ci-01/offline/device", null, ct);
                if (offline.Status >= 400) gaps.Add($"Offline registration producer: {offline.Status}/{offline.Code}");
            }
        }
        if (!string.IsNullOrWhiteSpace(storageConfigPath))
            await SeedStorageScenariosAsync(db, project.Id, guard, receipts, clock, storageConfigPath, gaps, ct);
        else gaps.Add("Storage config absent: reports/datasets/FIELD/repair require genuine uploaded bytes and verified provenance; not fabricated.");
        var ownedDevice = await db.Set<RoadGuardSystem.BusinessObjects.Offline.OfflineDeviceRegistration>()
            .SingleOrDefaultAsync(x => x.ProjectId == project.Id && x.DeviceId == Guid.Parse("8305e20c-0785-4012-a490-baf002852c02"), ct);
        if (ownedDevice is not null)
        {
            var ownedTasks = await db.FieldInspectionTasks.Where(x => x.ProjectId == project.Id && x.Instructions == "Synthetic dev/test FIELD task").ToListAsync(ct);
            foreach (var task in ownedTasks)
            {
                if (await db.Set<RoadGuardSystem.BusinessObjects.Offline.OfflineTaskSnapshot>().AnyAsync(x => x.TaskId == task.Id, ct)) continue;
                var offline = new RoadGuardSystem.Services.Offline.OfflineWorkflowService(
                    new RoadGuardSystem.Repositories.Implementations.Offline.OfflineWorkflowRepository(db, clock,
                        new RoadGuardSystem.Repositories.Implementations.Inspections.FieldInspectionWorkflowRepository(db, receipts, clock)), guard);
                var snapshot = await offline.ExecuteAsync(PostmanUserSeedStep.RepairCrewUserId, UserRoleCode.RepairCrew,
                    project.Id, "snapshot-create", new RoadGuardSystem.DTOs.Offline.OfflineSnapshotInput(ownedDevice.Id, task.Id),
                    null, "rg-ci-01/offline/snapshot/" + task.Id.ToString("N"), null, ct);
                if (snapshot.Status >= 400) gaps.Add($"Offline FIELD snapshot {task.Id}: {snapshot.Status}/{snapshot.Code}");
            }
        }
        var dispatcher = new RoadGuardSystem.Services.Messaging.H6NotificationDispatchService(
            new RoadGuardSystem.Repositories.Messaging.H6NotificationDispatchRepository(db, clock));
        var pending = await db.OutboxMessages.CountAsync(ct);
        for (var attempt = 0; attempt < pending; attempt++)
        {
            var dispatched = await dispatcher.ProcessOneAsync(ct);
            if (dispatched.Status == "IDLE") break;
        }
        gaps.Add("Offline crypto/package/sync records require genuine signed device payloads; no signatures, grants or admission receipts fabricated.");
        gaps.Add("Real AI inference is absent from this synthetic seed; current marked mock remains a separately configured producer.");
        var tables = await CoverageAsync(db, ct);
        return new("SYNTHETIC_DEV_TEST; SQL_PRODUCER_EXECUTION_ONLY; NOT_VERIFIED_REAL", project.Id, reference,
            [new { PostmanUserSeedStep.SupervisorUserId, Email = PostmanUserSeedStep.SupervisorEmail, Role = "SUPERVISOR" },
             new { PostmanUserSeedStep.ProjectManagerUserId, Email = PostmanUserSeedStep.ProjectManagerEmail, Role = "PM" },
             new { PostmanUserSeedStep.OperatorUserId, Email = PostmanUserSeedStep.OperatorEmail, Role = "DRONE_OPERATOR" },
             new { PostmanUserSeedStep.RepairCrewUserId, Email = PostmanUserSeedStep.RepairCrewEmail, Role = "REPAIR_CREW" },
             new { PostmanUserSeedStep.ReporterUserId, Email = PostmanUserSeedStep.ReporterEmail, Role = "REPORTER" }], tables, gaps,
            await ScenarioManifestAsync(db, project.Id, ct));
    }

    private static async Task EnsureMembershipAsync(RoadGuardDbContext db, Guid project, Guid user,
        UserRoleCode role, DateTimeOffset reference, CancellationToken ct)
    {
        // An ended/revoked membership is a valid user change, not permission to restore access.
        if (await db.ProjectMembers.AnyAsync(x => x.ProjectId == project && x.UserId == user, ct)) return;
        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project,
            UserId = user,
            RoleCode = role,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(reference.UtcDateTime)
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSurveyAsync(RoadGuardDbContext db, IProjectScopeGuard guard, IdempotencyOperationService receipts,
        Guid project, Guid route, Guid set, DateTimeOffset reference, CancellationToken ct)
    {
        if (await db.SurveyRequests.AnyAsync(x => x.ProjectId == project, ct)) return;
        var segments = await db.RoadSegments.Where(x => x.SegmentSetId == set).OrderBy(x => x.Sequence).Select(x => x.Id).ToArrayAsync(ct);
        var service = new SurveyV2Service(new SurveyV2PersistenceService(db, receipts), guard);
        var scope = new BandScopeDto[] { new(route, set, segments, "SURFACE") };
        var plan = await service.CreatePlanAsync(PostmanUserSeedStep.ProjectManagerUserId, UserRoleCode.ProjectManager,
            project, new(scope, reference.AddDays(7), "PERIODIC"), "rg-ci-01/survey/plan", null, ct);
        if (plan.Plan is null) throw new InvalidOperationException($"Survey plan: {plan.Status}");
        var task = await service.CreateTaskAsync(PostmanUserSeedStep.ProjectManagerUserId, UserRoleCode.ProjectManager,
            project, new(scope, "PERIODIC", PostmanUserSeedStep.OperatorUserId, reference.AddDays(8), null, plan.Plan.Id),
            "rg-ci-01/survey/task", null, ct);
        if (task.Task is null) throw new InvalidOperationException($"Survey task: {task.Status}");
    }

    private static async Task<IReadOnlyList<SeedTableCoverage>> CoverageAsync(RoadGuardDbContext db, CancellationToken ct)
    {
        var tables = await db.Database.SqlQueryRaw<string>("SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) AS [Value] FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0 ORDER BY s.name,t.name").ToListAsync(ct);
        var result = new List<SeedTableCoverage>();
        using var catalogStream = typeof(DevelopmentScenarios).Assembly.GetManifestResourceStream(
            "RoadGuardSystem.Seeder.SeedCoverageCatalog.json") ?? throw new InvalidOperationException("Seed coverage catalog missing.");
        var catalog = (await JsonSerializer.DeserializeAsync<CoverageCatalogRow[]>(catalogStream,
            ReadJson, ct))!.ToDictionary(x => x.Table);
        foreach (var table in tables)
        {
            // Names come exclusively from SQL Server QUOTENAME over its catalog, not user input.
            var count = await db.Database.SqlQueryRaw<long>("SELECT COUNT_BIG(*) AS [Value] FROM " + table).SingleAsync(ct);
            var key = table.Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);
            if (key.EndsWith(".__EFMigrationsHistory", StringComparison.Ordinal))
                result.Add(new(key, count, "EF migrations only", count == 0 ? "No applied migration; Seeder never writes migration history." : null));
            else if (catalog.TryGetValue(key, out var row))
                result.Add(new(key, count, row.Producer, count == 0 ? row.EmptyReason + " Scenario: " + row.Scenario : null,
                    row.Group, row.DependencyOrder, row.Prerequisites, row.Source));
            else throw new InvalidOperationException($"Actual table {key} lacks seed assessment; update catalog before accepting full seed coverage.");
        }
        return result;
    }

    private static async Task<IReadOnlyList<object>> ScenarioManifestAsync(RoadGuardDbContext db, Guid project, CancellationToken ct)
    {
        var rows = new List<object>
        {
            new { Id = project, Kind = "PROJECT", State = (await db.Projects.AsNoTracking().SingleAsync(x => x.Id == project, ct)).Status.ToString(),
                Actions = "Supervisor CreateProject -> genuine handover/warranty/PM membership; existing mutable state preserved", Evidence = "SYNTHETIC_DEV_TEST" }
        };
        foreach (var road in await db.RoadSections.AsNoTracking().Where(x => x.ProjectId == project).ToListAsync(ct))
            rows.Add(new { road.Id, Kind = "ROAD", road.Code, Actions = "PM profile/system/draft -> Supervisor confirm -> PM segment-set generate/publish", Limitation = "SampleOnly native CRS; no construction authority" });
        foreach (var task in await db.SurveyRequests.AsNoTracking().Where(x => x.ProjectId == project).ToListAsync(ct))
            rows.Add(new
            {
                task.Id,
                Kind = "SURVEY_TASK",
                State = task.Status.ToString(),
                Produced = "PM CreatePlanAsync/CreateTaskAsync",
                NextRequest = "Assigned DRONE_OPERATOR GetTaskAsync -> AcceptTaskAsync using current Version -> UploadService SURVEY_VIDEO/TELEMETRY -> SubmitDatasetAsync",
                ActorId = PostmanUserSeedStep.OperatorUserId,
                Version = Convert.ToBase64String(task.RowVersion)
            });
        foreach (var report in await db.Reports.AsNoTracking().Where(x => x.ReporterUserId == PostmanUserSeedStep.ReporterUserId && x.Description.StartsWith("RG-CI-01 synthetic reporter graph")).ToListAsync(ct))
        {
            var link = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>().AsNoTracking()
                .SingleAsync(x => x.ReportId == report.Id && x.EndedAt == null, ct);
            rows.Add(new
            {
                report.Id,
                Kind = "REPORT",
                report.Description,
                CaseId = link.CaseId,
                Produced = "ReporterEvidenceService multipart uploaded bytes -> actual verifier -> ReporterReportService.CreateAsync",
                ActorId = PostmanUserSeedStep.ReporterUserId,
                Limitation = "Synthetic manual dev/test facts; no real observation"
            });
        }
        foreach (var incident in await db.IncidentCases.AsNoTracking().Where(x => x.ProjectId == project).ToListAsync(ct))
        {
            var conclusions = await db.Set<RoadGuardSystem.BusinessObjects.Cases.CaseConclusion>().AsNoTracking()
                .Where(x => EF.Property<Guid>(x, "CaseId") == incident.Id).Select(x => x.Id).ToArrayAsync(ct);
            var publications = await db.Set<RoadGuardSystem.BusinessObjects.Cases.CasePublication>().AsNoTracking()
                .Where(x => x.CaseId == incident.Id).Select(x => x.Id).ToArrayAsync(ct);
            rows.Add(new
            {
                incident.Id,
                Kind = "CASE",
                State = incident.Status.ToString(),
                ConclusionIds = conclusions,
                PublicationIds = publications,
                ObservedStages = publications.Length > 0 ? "triaged;concluded;published" : conclusions.Length > 0 ? "triaged;concluded" : "triaged",
                ActorId = PostmanUserSeedStep.ProjectManagerUserId,
                NextRequest = publications.Length > 0 ? "None; existing publication preserved" : "Read current case; conclude only when current source prerequisites permit; publish with current Version"
            });
        }
        foreach (var task in await db.FieldInspectionTasks.AsNoTracking().Where(x => x.ProjectId == project).ToListAsync(ct))
            rows.Add(new
            {
                task.Id,
                Kind = "FIELD_TASK",
                State = task.Status.ToString(),
                task.DefectId,
                Produced = "FieldInspectionWorkflowService create; current State records actual completed transitions",
                ActorId = PostmanUserSeedStep.RepairCrewUserId,
                NextRequest = "Assigned REPAIR_CREW get -> accept/start/submit where current state permits; PM review requires authentic submission readiness",
                Limitation = "Synthetic task; no fabricated verified FIELD result"
            });
        return rows;
    }

    private static async Task SeedStorageScenariosAsync(RoadGuardDbContext db, Guid project, IProjectScopeGuard guard,
        IdempotencyOperationService receipts, TimeProvider clock, string configPath, List<string> gaps, CancellationToken ct)
    {
        var options = JsonSerializer.Deserialize<MinioStorageOptions>(await File.ReadAllTextAsync(configPath, ct),
            ReadJson) ?? throw new InvalidOperationException("Storage options missing.");
        // Credentials stay in options only; never place them or signed URLs in manifest or errors.
        var endpoint = options.Endpoint.Contains("://", StringComparison.Ordinal) ? options.Endpoint :
            (options.UseSsl ? "https://" : "http://") + options.Endpoint;
        using var client = new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        try
        {
            var buckets = await client.ListBucketsAsync(ct);
            if (buckets.Buckets?.Any(x => x.BucketName == options.BucketName) != true)
                await client.PutBucketAsync(new PutBucketRequest { BucketName = options.BucketName, UseClientRegion = true }, ct);
        }
        catch (Exception ex) when (ex is AmazonS3Exception or HttpRequestException)
        { throw new InvalidOperationException("Requested isolated object storage is unavailable; storage seed gate failed."); }
        var storage = new MinioUploadObjectStorage(Options.Create(options));
        var uploads = new UploadPersistenceService(db, receipts, storage);
        var producer = new AnhHuyProducerService(new AnhHuyFactsRepository(db), new GeometryWorkflowPersistenceService(db, receipts, clock), guard);
        var uploadOptions = Options.Create(new UploadSessionOptions());
        var evidence = new ReporterEvidenceService(uploads, producer, new ReporterEvidencePersistenceService(db), clock, uploadOptions);
        var verifier = new UploadService(uploads, guard, clock, uploadOptions);
        var intake = new ReporterReportService(producer, new ReporterReportRepository(db), receipts);
        await SeedDatasetAsync(db, project, guard, receipts, verifier, gaps, ct);
        var reporter = PostmanUserSeedStep.ReporterUserId;
        var bytes = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAX/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAF//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABBQJ//8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAgBAwEBPwF//8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAgBAgEBPwF//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQAGPwJ//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABPyF//9oADAMBAAIAAwAAABD/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oACAEDAQE/EH//xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oACAECAQE/EH//xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oACAEBAAE/EH//2Q==");
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        for (var graph = 1; graph <= 3; graph++)
        {
            var description = $"RG-CI-01 synthetic reporter graph {graph}";
            var existingReport = await db.Reports.SingleOrDefaultAsync(x => x.ReporterUserId == reporter && x.Description == description, ct);
            if (existingReport is not null)
            {
                if (graph > 1)
                {
                    var owned = await new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(db).GetReportSourceAsync(existingReport.Id, ct);
                    await SeedCaseAsync(db, project, graph, new(existingReport.Id, existingReport.Description, existingReport.ReceivedAt,
                        owned!.ReportVersion, owned.Evidence.Select(x => x.EvidenceId).ToArray()), producer, guard, receipts, clock, gaps, ct);
                }
                continue;
            }
            var key = $"rg-ci-01/report/{graph}";
            var created = await evidence.CreateAsync(reporter, UserRoleCode.Reporter,
                new($"synthetic-{graph}.jpg", "image/jpeg", bytes.Length, checksum), key + "/upload", null, ct);
            var initial = created.Session ?? throw new InvalidOperationException($"Reporter upload create rejected: {created.Status}");
            var session = (await evidence.GetSessionAsync(reporter, UserRoleCode.Reporter, initial.Id, ct)).Session
                ?? throw new InvalidOperationException("Reporter current upload session missing.");
            if (session.Status != "VERIFIED")
            {
                var urls = await evidence.GetPartUrlsAsync(reporter, UserRoleCode.Reporter, session.Id,
                    new([1]), key + "/parts", ct);
                var url = urls.PartUrls?.Parts.Single().Url ?? throw new InvalidOperationException($"Reporter upload part rejected: {urls.Status}");
                using var http = new HttpClient();
                using var content = new ByteArrayContent(bytes);
                using var response = await http.PutAsync(url, content, ct);
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Genuine multipart part upload failed.");
                var etag = response.Headers.ETag?.Tag ?? throw new InvalidOperationException("Multipart response ETag missing.");
                session = (await evidence.GetSessionAsync(reporter, UserRoleCode.Reporter, session.Id, ct)).Session!;
                var complete = await evidence.CompleteAsync(reporter, UserRoleCode.Reporter, session.Id,
                    new([new CompletedUploadPartDto(1, etag)], checksum), key + "/complete", session.Version, null, ct);
                if (complete.Session is null) throw new InvalidOperationException($"Reporter upload complete rejected: {complete.Status}");
                await verifier.ProcessOneVerificationAsync(ct);
            }
            db.ChangeTracker.Clear();
            var verified = await evidence.GetSessionAsync(reporter, UserRoleCode.Reporter, session.Id, ct);
            if (verified.Session?.Status != "VERIFIED") throw new InvalidOperationException("Real storage verification did not produce VERIFIED upload.");
            var report = await intake.CreateAsync(reporter, UserRoleCode.Reporter,
                new(description, [new(session.FileId, verified.Session.Version, "UNKNOWN")]), key + "/intake", null, ct);
            if (report.Report is null) throw new InvalidOperationException($"Reporter intake rejected: {report.Status}");
            if (graph > 1)
                await SeedCaseAsync(db, project, graph, report.Report, producer, guard, receipts, clock, gaps, ct);
        }
    }

    private static async Task SeedDatasetAsync(RoadGuardDbContext db, Guid project, IProjectScopeGuard guard,
        IdempotencyOperationService receipts, UploadService uploads, List<string> gaps, CancellationToken ct)
    {
        var task = await db.SurveyRequests.SingleOrDefaultAsync(x => x.ProjectId == project, ct);
        if (task is null || await db.Surveys.AnyAsync(x => x.SurveyRequestId == task.Id, ct)) return;
        var actor = PostmanUserSeedStep.OperatorUserId;
        var service = new SurveyV2Service(new SurveyV2PersistenceService(db, receipts), guard);
        var read = await service.GetTaskAsync(actor, UserRoleCode.DroneOperator, task.Id, ct);
        if (read.Task is null) { gaps.Add($"Dataset task read: {read.Status}; current assignment preserved."); return; }
        if (read.Task.Status == "NEW_ASSIGNED")
        {
            var accepted = await service.AcceptTaskAsync(actor, UserRoleCode.DroneOperator, task.Id,
                "rg-ci-01/survey/accept", read.Task.Version, null, ct);
            if (accepted.Task is null) { gaps.Add($"Survey accept: {accepted.Status}"); return; }
        }
        else if (read.Task.Status is not ("ACCEPTED" or "IN_PROGRESS")) return;
        using var video = typeof(DevelopmentScenarios).Assembly.GetManifestResourceStream("RoadGuardSystem.Seeder.synthetic-survey.mp4")
            ?? throw new InvalidOperationException("Synthetic generated MP4 fixture missing.");
        using var buffer = new MemoryStream();
        await video.CopyToAsync(buffer, ct);
        var videoId = await UploadProjectFileAsync(uploads, actor, UserRoleCode.DroneOperator, project, task.Id,
            "SURVEY_VIDEO", "synthetic-generated-black.mp4", "video/mp4", buffer.ToArray(), "rg-ci-01/survey/video", ct);
        var telemetry = System.Text.Encoding.UTF8.GetBytes("1\n00:00:00,000 --> 00:00:01,000\nRG-CI-01 synthetic telemetry; position UNKNOWN\n");
        var telemetryId = await UploadProjectFileAsync(uploads, actor, UserRoleCode.DroneOperator, project, task.Id,
            "TELEMETRY", "synthetic-unknown-position.srt", "application/x-subrip", telemetry, "rg-ci-01/survey/telemetry", ct);
        read = await service.GetTaskAsync(actor, UserRoleCode.DroneOperator, task.Id, ct);
        var dataset = await service.SubmitDatasetAsync(actor, UserRoleCode.DroneOperator, task.Id,
            new([videoId], [telemetryId], (await db.Projects.SingleAsync(x => x.Id == project, ct)).CreatedAt, DroneDeviceSeedStep.FixtureDeviceId, read.Task!.Scope,
                [new(videoId, telemetryId, 0)]), "rg-ci-01/survey/dataset", read.Task.Version, null, ct);
        if (dataset.Dataset is null) gaps.Add($"Survey dataset submission: {dataset.Status}");
    }

    private static async Task<Guid> UploadProjectFileAsync(UploadService uploads, Guid actor, UserRoleCode role,
        Guid project, Guid target, string purpose, string name, string media, byte[] bytes, string key, CancellationToken ct)
    {
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var created = await uploads.CreateAsync(actor, role, new(purpose, project, target, name, media, bytes.Length, checksum), key, null, ct);
        if (created.Session is null) throw new InvalidOperationException($"{purpose} upload create: {created.Status}");
        var session = (await uploads.GetSessionAsync(actor, role, created.Session.Id, ct)).Session!;
        if (session.Status != "VERIFIED")
        {
            var parts = await uploads.GetPartUrlsAsync(actor, role, session.Id, new([1]), key + "/parts", ct);
            if (parts.PartUrls is null) throw new InvalidOperationException($"{purpose} part URLs: {parts.Status}");
            using var http = new HttpClient();
            using var content = new ByteArrayContent(bytes);
            using var response = await http.PutAsync(parts.PartUrls.Parts.Single().Url, content, ct);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"{purpose} real object upload failed.");
            var etag = response.Headers.ETag?.Tag ?? throw new InvalidOperationException("Real multipart ETag missing.");
            session = (await uploads.GetSessionAsync(actor, role, session.Id, ct)).Session!;
            var complete = await uploads.CompleteAsync(actor, role, session.Id, new([new(1, etag)], checksum),
                key + "/complete", session.Version, null, ct);
            if (complete.Session is null) throw new InvalidOperationException($"{purpose} upload complete: {complete.Status}");
            await uploads.ProcessOneVerificationAsync(ct);
        }
        var verified = await uploads.GetSessionAsync(actor, role, session.Id, ct);
        if (verified.Session?.Status != "VERIFIED") throw new InvalidOperationException($"{purpose} genuine verification failed.");
        return session.FileId;
    }

    private static async Task SeedCaseAsync(RoadGuardDbContext db, Guid project, int graph,
        ReporterReportResponseDto report, AnhHuyProducerService producer, IProjectScopeGuard guard,
        IdempotencyOperationService receipts, TimeProvider clock, List<string> gaps, CancellationToken ct)
    {
        var pm = PostmanUserSeedStep.ProjectManagerUserId;
        var supervisor = PostmanUserSeedStep.SupervisorUserId;
        var repository = new RoadGuardSystem.Repositories.Implementations.Cases.CaseWorkflowRepository(db);
        var cases = new RoadGuardSystem.Services.Implementations.Cases.CaseWorkflowService(repository, guard, producer, receipts);
        var caseId = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .Where(x => x.ReportId == report.Id && x.EndedAt == null).Select(x => x.CaseId).SingleAsync(ct);
        var incident = await db.IncidentCases.SingleAsync(x => x.Id == caseId, ct);
        var road = await db.RoadSections.SingleOrDefaultAsync(x => x.ProjectId == project && x.Code == RoadCode, ct);
        var route = road is null ? null : await db.RoadSectionVersions.SingleOrDefaultAsync(x => x.RoadSectionId == road.Id && x.IsCurrent, ct);
        var set = route is null ? null : await db.RoadSegmentSets.SingleOrDefaultAsync(x => x.RoadSectionVersionId == route.Id && x.Status == "PUBLISHED", ct);
        if (route is null || set is null) { gaps.Add($"Case graph {graph}: sample route/set producer incomplete."); return; }
        var geometry = await producer.ResolveGeometryAsync(pm, UserRoleCode.ProjectManager, project, route.Id, set.Id, cancellationToken: ct);
        if (geometry.Facts is null) { gaps.Add($"Case graph {graph}: geometry {geometry.Status}"); return; }
        var read = await cases.ReadAsync(supervisor, UserRoleCode.Supervisor, incident.Id, ct);
        if (read.Case!.ProjectId is null)
        {
            var triage = await cases.CommandAsync(supervisor, UserRoleCode.Supervisor,
                new(incident.Id, read.Case!.Version, "triage", "Synthetic existing-evidence demonstration", project,
                    RoadGuardSystem.BusinessObjects.Cases.CaseVerificationMethod.ExistingEvidence,
                    RouteVersionId: route.Id, SegmentSetId: set.Id, GeometryVersion: geometry.Facts.Version),
                $"rg-ci-01/case/{graph}/triage", null, ct);
            if (triage.Status >= 400) { gaps.Add($"Case graph {graph} triage: {triage.Status}/{triage.Code}"); return; }
        }
        else if (read.Case.ProjectId != project) { gaps.Add($"Case graph {graph}: user changed project scope; preserved."); return; }
        var source = await producer.ResolveCandidateSourceAsync(pm, UserRoleCode.ProjectManager, project,
            RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report.Id, cancellationToken: ct);
        if (source.Facts is null) { gaps.Add($"Case graph {graph} candidate source: {source.Status}"); return; }
        var decisions = new RoadGuardSystem.Services.Implementations.Defects.CandidateDecisionService(
            new RoadGuardSystem.Repositories.Implementations.Defects.CandidateDecisionRepository(db), repository, producer, [], guard, receipts);
        var existingDecision = await db.SourceDecisions.SingleOrDefaultAsync(x => x.Source.Id == report.Id && x.Source.Kind == RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, ct);
        Guid defectId;
        if (existingDecision is not null)
        {
            var existingDefect = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>()
                .Where(x => x.DecisionId == existingDecision.Id && x.EndedAt == null).Select(x => (Guid?)x.DefectId).SingleOrDefaultAsync(ct);
            if (existingDefect is null) { gaps.Add($"Case graph {graph}: existing source disposition preserved."); return; }
            defectId = existingDefect.Value;
        }
        else
        {
            var decision = await decisions.DecideAsync(pm, UserRoleCode.ProjectManager, project,
                new("REPORT", report.Id, source.Facts.DomainFacts.Source.SourceVersion, geometry.Facts.Version, "KEEP_NEW", null, null,
                    new(PostmanScenarioSeedStep.DefectTypeCode, null, "MEDIUM", route.Id), "Synthetic candidate decision"),
                $"rg-ci-01/case/{graph}/decision", null, ct);
            if (decision.Decision?.DefectId is not Guid newDefect) { gaps.Add($"Case graph {graph} KeepNew: {decision.Status}/{decision.Code}"); return; }
            defectId = newDefect;
        }
        if (graph == 2)
        {
            var defect = await db.Defects.SingleAsync(x => x.Id == defectId, ct);
            if (await db.FieldInspectionTasks.AnyAsync(x => x.DefectId == defectId, ct)) return;
            var field = new RoadGuardSystem.Services.Implementations.Inspections.FieldInspectionWorkflowService(
                new RoadGuardSystem.Repositories.Implementations.Inspections.FieldInspectionWorkflowRepository(db, receipts, clock), guard);
            var created = await field.ExecuteAsync(pm, UserRoleCode.ProjectManager, project, null, "create",
                new RoadGuardSystem.DTOs.Inspections.FieldTaskCreateInput(defectId,
                    Convert.ToBase64String(db.Entry(defect).Property<byte[]>("RowVersion").CurrentValue!), null,
                    "REPORTER", route.Id, set.Id, null, null, "PRE_MEASUREMENT", 1, "{\"dimensions\":[\"LENGTH\"]}", "Synthetic dev/test FIELD task",
                    PostmanUserSeedStep.RepairCrewUserId, clock.GetUtcNow().AddDays(7), CrsProfileRevisionId: route.CrsProfileRevisionId),
                "rg-ci-01/field/create", null, ct);
            if (created.Status >= 400) gaps.Add($"FIELD task producer: {created.Status}/{created.Code}");
            else if (created.Value is RoadGuardSystem.DTOs.Inspections.FieldTaskView createdTask)
            {
                var accepted = await field.ExecuteAsync(PostmanUserSeedStep.RepairCrewUserId, UserRoleCode.RepairCrew,
                    project, createdTask.Id, "accept", new RoadGuardSystem.DTOs.Inspections.FieldTaskActionInput("Synthetic crew acceptance"),
                    "rg-ci-01/field/accept", createdTask.Version, ct);
                if (accepted.Status >= 400) gaps.Add($"FIELD accept producer: {accepted.Status}/{accepted.Code}");
                else
                {
                    var current = await field.ExecuteAsync(PostmanUserSeedStep.RepairCrewUserId, UserRoleCode.RepairCrew,
                        project, createdTask.Id, "get", null, null, null, ct);
                    var started = await field.ExecuteAsync(PostmanUserSeedStep.RepairCrewUserId, UserRoleCode.RepairCrew,
                        project, createdTask.Id, "start", new RoadGuardSystem.DTOs.Inspections.FieldStartInput(
                            Guid.Parse("8305e20c-0785-4012-a490-baf002852c03"), clock.GetUtcNow()),
                        "rg-ci-01/field/start", current.Version, ct);
                    if (started.Status >= 400) gaps.Add($"FIELD start producer: {started.Status}/{started.Code}");
                }
            }
            return;
        }
        var fresh = await cases.ReadAsync(pm, UserRoleCode.ProjectManager, incident.Id, ct);
        if (await db.Set<RoadGuardSystem.BusinessObjects.Cases.CasePublication>().AnyAsync(x => x.CaseId == incident.Id, ct)) return;
        var defectWorkflow = new RoadGuardSystem.Services.Implementations.Defects.DefectWorkflowService(
            new RoadGuardSystem.Repositories.Implementations.Defects.DefectWorkflowRepository(db),
            new RoadGuardSystem.Repositories.Implementations.Defects.CandidateDecisionRepository(db), repository, producer, guard, receipts);
        var defectRead = await defectWorkflow.ReadAsync(pm, UserRoleCode.ProjectManager, project, defectId, ct);
        if (defectRead.Defect?.Status == "OPEN")
        {
            var verifiedDefect = await defectWorkflow.VerifyAsync(pm, UserRoleCode.ProjectManager, project, defectId,
                new("CONFIRM", "EXISTING_EVIDENCE", report.EvidenceIds.ToArray(), "Synthetic demo evidence reviewed by dev/test PM"),
                $"rg-ci-01/case/{graph}/verify", '"' + defectRead.Defect.Version + '"', null, ct);
            if (verifiedDefect.Status >= 400) { gaps.Add($"Case graph {graph} defect verification: {verifiedDefect.Status}/{verifiedDefect.Code}"); return; }
        }
        if (fresh.Case!.Conclusion is null)
        {
            var conclusion = await cases.CommandAsync(pm, UserRoleCode.ProjectManager,
                new(incident.Id, fresh.Case!.Version, "conclude", "Synthetic existing-evidence conclusion",
                    Outcome: RoadGuardSystem.BusinessObjects.Cases.CaseConclusionOutcome.Confirmed,
                    DefectIds: [defectId], EvidenceIds: report.EvidenceIds), $"rg-ci-01/case/{graph}/conclude", null, ct);
            if (conclusion.Status >= 400) { gaps.Add($"Case graph {graph} conclude: {conclusion.Status}/{conclusion.Code}"); return; }
        }
        fresh = await cases.ReadAsync(pm, UserRoleCode.ProjectManager, incident.Id, ct);
        var published = await cases.CommandAsync(pm, UserRoleCode.ProjectManager,
            new(incident.Id, fresh.Case!.Version, "publish", "Synthetic publication to example.test demo Reporter",
                ReportIds: [report.Id], DefectIds: [defectId], EvidenceIds: report.EvidenceIds),
            $"rg-ci-01/case/{graph}/publish", null, ct);
        if (published.Status >= 400) gaps.Add($"Case graph {graph} publish: {published.Status}/{published.Code}");
    }
}
