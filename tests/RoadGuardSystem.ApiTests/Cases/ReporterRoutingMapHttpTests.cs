using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Models.Huy01;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ReporterRoutingMapHttpTests(AuthenticationSqlServerFixture sql) : IDisposable
{
    private readonly InMemoryLogProvider _logs = new();
    public void Dispose() => _logs.Dispose();
    private AuthenticationWebApplicationFactory Factory() => new(sql.ConnectionString,
        configureTestServices: services => services.AddSingleton<ILoggerProvider>(_logs));
    [Fact]
    public async Task RoutingSwaggerHasTypedContractsCookieAlternativesAndConditionalHeaders()
    {
        await using var factory = new CustomWebApplicationFactory("Development");
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var (path, method) in new[] { ("/api/v1/company/road-map", "get"), ("/api/v1/cases/{caseId}/routing-candidates", "get"),
                     ("/api/v1/cases/{caseId}/routing-decisions", "get"), ("/api/v1/cases/{caseId}/routing-decisions", "post") })
        {
            var operation = document.GetProperty("paths").GetProperty(path).GetProperty(method);
            Assert.Contains(operation.GetProperty("security").EnumerateArray(), s => s.TryGetProperty("Bearer", out _));
            Assert.Contains(operation.GetProperty("security").EnumerateArray(), s => s.TryGetProperty("WebSession", out _) &&
                (method == "get" ? !s.TryGetProperty("CsrfToken", out _) : s.TryGetProperty("CsrfToken", out _)));
            Assert.True(operation.GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema").TryGetProperty("$ref", out _));
        }
        var mapOperation = document.GetProperty("paths").GetProperty("/api/v1/company/road-map").GetProperty("get");
        Assert.Contains(mapOperation.GetProperty("parameters").EnumerateArray(), p => p.GetProperty("name").GetString() == "If-None-Match");
        Assert.True(mapOperation.GetProperty("responses").GetProperty("200").GetProperty("headers").TryGetProperty("ETag", out _));
        var decisionOperation = document.GetProperty("paths").GetProperty("/api/v1/cases/{caseId}/routing-decisions").GetProperty("post");
        Assert.True(decisionOperation.GetProperty("responses").GetProperty("200").GetProperty("headers").TryGetProperty("ETag", out _));
        var schemas = document.GetProperty("components").GetProperty("schemas");
        Assert.Equal(100, schemas.GetProperty("RoutingWholeCaseAttestation").GetProperty("properties").GetProperty("reports").GetProperty("maxItems").GetInt32());
        Assert.Equal(20, schemas.GetProperty("RoutingReportAttestation").GetProperty("properties").GetProperty("sourceReferences").GetProperty("maxItems").GetInt32());
        var output = Environment.GetEnvironmentVariable("ROADGUARD_ROUTING_OPENAPI_OUTPUT");
        if (!string.IsNullOrWhiteSpace(output)) await File.WriteAllTextAsync(output, document.GetRawText());
    }

    [Fact]
    public async Task MapUsesRealGeometryIntersectionStablePagesAndCurrentSupervisorAuthority()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager);
        var first = await Route(32648, false);
        var second = await Route(32649, true);
        var unsupported = await Route(32648, false, native: true);
        await using var factory = Factory();
        using var client = await WebCookieTestClient.Login(factory, sup.Email!);
        var seen = new HashSet<Guid>();
        string? next = null;
        do
        {
            var page = await Get(client, "/api/v1/company/road-map?limit=20" + (next is null ? "" : "&cursor=" + Uri.EscapeDataString(next)));
            foreach (var feature in page.GetProperty("features").EnumerateArray()) Assert.True(seen.Add(feature.GetProperty("id").GetGuid()));
            next = page.GetProperty("nextCursor").GetString();
        } while (next is not null);
        Assert.Contains(first.RouteId, seen); Assert.Contains(second.RouteId, seen); Assert.DoesNotContain(unsupported.RouteId, seen);
        // Both endpoints lie outside this thin box; the actual line crosses it.
        var intersectingFeatures = new List<JsonElement>();
        var unavailableRoutes = new List<JsonElement>();
        next = null;
        do
        {
            var intersect = await Get(client, "/api/v1/company/road-map?bbox=104.9999,9.9999,105.0001,10.0001" +
                (next is null ? "" : "&cursor=" + Uri.EscapeDataString(next)));
            intersectingFeatures.AddRange(intersect.GetProperty("features").EnumerateArray().Select(f => f.Clone()));
            unavailableRoutes.AddRange(intersect.GetProperty("unavailableRoutes").EnumerateArray().Select(r => r.Clone()));
            next = intersect.GetProperty("nextCursor").GetString();
        } while (next is not null);
        var featureA = Assert.Single(intersectingFeatures, f => f.GetProperty("id").GetGuid() == first.RouteId);
        var xy = featureA.GetProperty("geometry").GetProperty("coordinates")[0];
        Assert.InRange(xy[0].GetDouble(), 104.98, 105); Assert.InRange(xy[1].GetDouble(), 9.99999, 10.00001);
        Assert.Contains(unavailableRoutes, r => r.GetProperty("routeVersionId").GetGuid() == unsupported.RouteId);
        using var response = await client.GetAsync("/api/v1/company/road-map?limit=1");
        var tag = response.Headers.ETag!.Tag;
        var one = await response.Content.ReadFromJsonAsync<JsonElement>();
        var cursor = one.GetProperty("nextCursor").GetString()!;
        using var cached = new HttpRequestMessage(HttpMethod.Get, "/api/v1/company/road-map?limit=1");
        cached.Headers.TryAddWithoutValidation("If-None-Match", tag);
        Assert.Equal(HttpStatusCode.NotModified, (await client.SendAsync(cached)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/company/road-map?limit=2&cursor=" + Uri.EscapeDataString(cursor))).StatusCode);
        using var manager = await WebCookieTestClient.Login(factory, pm.Email!);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync("/api/v1/company/road-map")).StatusCode);
        var otherSupervisor = await User(UserRoleCode.Supervisor);
        using var otherReader = await WebCookieTestClient.Login(factory, otherSupervisor.Email!);
        Assert.Equal(HttpStatusCode.BadRequest, (await otherReader.GetAsync("/api/v1/company/road-map?limit=1&cursor=" + Uri.EscapeDataString(cursor))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/company/road-map?bbox=105,10,104,11")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/company/road-map?limit=101")).StatusCode);
        await Route(32648, false);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync("/api/v1/company/road-map?limit=1&cursor=" + Uri.EscapeDataString(cursor))).StatusCode);
        await using (var db = sql.CreateDbContext())
            await db.Users.Where(u => u.Id == sup.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, UserStatus.Suspended));
        using var revoked = new HttpRequestMessage(HttpMethod.Get, "/api/v1/company/road-map?limit=1");
        revoked.Headers.TryAddWithoutValidation("If-None-Match", tag);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(revoked)).StatusCode);
    }

    [Fact]
    public async Task CandidatesPinTheSelectedEvidenceAndKeepDistanceSeparateFromResponsibility()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var reporter = await User(UserRoleCode.Reporter);
        var source = await Intake(reporter.Id);
        var route = await Route(32648, false);
        await using var factory = Factory();
        using var client = await Bearer(factory, sup.Email!);
        var a = await Get(client, CandidatePath(source, source.EvidenceA));
        var b = await Get(client, CandidatePath(source, source.EvidenceB));
        Assert.Equal(105m, a.GetProperty("source").GetProperty("longitude").GetDecimal());
        Assert.Equal(106m, b.GetProperty("source").GetProperty("longitude").GetDecimal());
        Assert.NotEqual(a.GetProperty("evaluationVersion").GetString(), b.GetProperty("evaluationVersion").GetString());
        var candidate = await Candidate(client, source, source.EvidenceA, route.RouteId);
        Assert.InRange(candidate.GetProperty("distanceMeters").GetDouble(), 0, .01);
        Assert.Equal("UNKNOWN", candidate.GetProperty("spatialRelation").GetString());
        Assert.Equal("UNKNOWN", candidate.GetProperty("responsibility").GetString());
        Assert.Equal("CLIENT_CLAIMED", a.GetProperty("source").GetProperty("captureTimeProvenance").GetString());
        Assert.Equal(JsonValueKind.Null, a.GetProperty("source").GetProperty("accuracyMeters").ValueKind);
        var missing = await Get(client, CandidatePath(source, source.MissingEvidence));
        Assert.Empty(missing.GetProperty("candidates").EnumerateArray());
        Assert.Contains("GPS_MISSING", missing.GetProperty("missingReasons").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(CandidatePath(source, Guid.NewGuid()))).StatusCode);
        using var owner = await Bearer(factory, reporter.Email!);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync(CandidatePath(source, source.EvidenceA))).StatusCode);
        await using (var db = sql.CreateDbContext())
            await db.UploadSessions.Where(u => u.FileId == source.FileA).ExecuteUpdateAsync(u => u.SetProperty(x => x.StorageUploadId, "source-version-changed"));
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await client.GetAsync(CandidatePath(source, source.EvidenceA))).StatusCode);
    }

    [Fact]
    public async Task IntakeDecisionsReplayReconsiderInvalidateOnSupplementAndNeverPublishOrCreateWork()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var reporter = await User(UserRoleCode.Reporter);
        var source = await Intake(reporter.Id);
        await using var factory = Factory();
        using var client = await WebCookieTestClient.Login(factory, sup.Email!);
        var version = (await Get(client, $"/api/v1/cases/{source.CaseId}")).GetProperty("version").GetString()!;
        var body = new { outcome = "NEEDS_LOCATION_EVIDENCE", reason = "Selected image has no position; request source clarification", reportId = source.ReportId, evidenceId = source.MissingEvidence };
        var key = Guid.NewGuid().ToString("N");
        var workBefore = await WorkCounts();
        var first = await Write(client, source.CaseId, "routing-decisions", version, key, body);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var saved = await first.Content.ReadFromJsonAsync<JsonElement>();
        var replay = await Write(client, source.CaseId, "routing-decisions", version, key, body);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(saved, await replay.Content.ReadFromJsonAsync<JsonElement>(), JsonElementComparer.Instance);
        var read = await Get(client, $"/api/v1/cases/{source.CaseId}");
        Assert.Equal("UNASSIGNED", read.GetProperty("status").GetString());
        Assert.Equal("EFFECTIVE", read.GetProperty("routingDisposition").GetProperty("state").GetString());
        using var fresh = await WebCookieTestClient.Login(factory, sup.Email!);
        var history = await Get(fresh, $"/api/v1/cases/{source.CaseId}/routing-decisions");
        Assert.Single(history.GetProperty("items").EnumerateArray());
        var outside = await Write(client, source.CaseId, "routing-decisions", read.GetProperty("version").GetString()!, Guid.NewGuid().ToString("N"),
            new { outcome = "OUTSIDE_COMPANY_SCOPE", reason = "No GPS is not enough to declare outside", reportId = source.ReportId });
        Assert.Equal(HttpStatusCode.Conflict, outside.StatusCode);
        await using (var db = sql.CreateDbContext())
        {
            var report = await db.Reports.Include(r => r.OriginalEvidence).Include(r => r.Supplements).SingleAsync(r => r.Id == source.ReportId);
            var original = report.OriginalEvidence[0];
            report.AddSupplement(Guid.NewGuid(), "New position evidence",
                [VerifiedEvidenceReference.Create(Guid.NewGuid(), original.FileId, original.FileVersion, reporter.Id,
                    EvidenceCaptureMetadata.Create(DateTimeOffset.UtcNow, EvidenceLocationSource.Exif, 12m, 106m))], DateTimeOffset.UtcNow);
            db.Entry(report).Property<long>("Revision").CurrentValue++;
            await db.SaveChangesAsync();
        }
        var stale = await Get(fresh, $"/api/v1/cases/{source.CaseId}");
        Assert.Equal("REVIEW_REQUIRED", stale.GetProperty("routingDisposition").GetProperty("state").GetString());
        var second = await Write(fresh, source.CaseId, "routing-decisions", stale.GetProperty("version").GetString()!, Guid.NewGuid().ToString("N"), body);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(saved.GetProperty("decision").GetProperty("decisionId").GetGuid(), secondBody.GetProperty("decision").GetProperty("supersedesDecisionId").GetGuid());
        await using var proof = sql.CreateDbContext();
        Assert.Equal(2, await proof.Set<CaseRoutingEvent>().CountAsync(e => e.CaseId == source.CaseId));
        Assert.Empty(await proof.Set<CasePublication>().Where(p => EF.Property<Guid>(p, "CaseId") == source.CaseId).ToArrayAsync());
        Assert.Equal(IncidentCaseStatus.Unassigned, (await proof.IncidentCases.SingleAsync(c => c.Id == source.CaseId)).Status);
        Assert.Equal(workBefore, await WorkCounts());
    }

    [Fact]
    public async Task FieldTriageWithoutGpsSupersedesIntakeButDoesNotPermitSupervisorProjectTransfer()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager);
        var reporter = await User(UserRoleCode.Reporter);
        var route = await Route(32648, false, pm: pm.Id);
        var source = await Intake(reporter.Id);
        await using var factory = Factory();
        using var client = await WebCookieTestClient.Login(factory, sup.Email!);
        var read = await Get(client, $"/api/v1/cases/{source.CaseId}");
        var decision = await Write(client, source.CaseId, "routing-decisions", read.GetProperty("version").GetString()!, Guid.NewGuid().ToString(),
            new { outcome = "NEEDS_LOCATION_EVIDENCE", reason = "Field survey needed", reportId = source.ReportId });
        Assert.Equal(HttpStatusCode.OK, decision.StatusCode);
        read = await Get(client, $"/api/v1/cases/{source.CaseId}");
        var body = new { projectId = route.ProjectId, verificationMethod = "FIELD", reason = "Grounded project scope; confirm missing GPS in field", reportId = source.ReportId, evidenceId = source.MissingEvidence };
        var key = Guid.NewGuid().ToString();
        var version = read.GetProperty("version").GetString()!;
        var triage = await Write(client, source.CaseId, "triage", version, key, body);
        Assert.Equal(HttpStatusCode.OK, triage.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Write(client, source.CaseId, "triage", version, key, body)).StatusCode);
        read = await Get(client, $"/api/v1/cases/{source.CaseId}");
        Assert.False(read.TryGetProperty("routingDisposition", out _));
        Assert.Equal(source.MissingEvidence, read.GetProperty("routingSource").GetProperty("source").GetProperty("evidenceId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await Write(client, source.CaseId, "triage", read.GetProperty("version").GetString()!, Guid.NewGuid().ToString(), body)).StatusCode);
        using var manager = await WebCookieTestClient.Login(factory, pm.Email!);
        Assert.Equal(HttpStatusCode.OK, (await Write(manager, source.CaseId, "triage", read.GetProperty("version").GetString()!, Guid.NewGuid().ToString(), body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"/api/v1/cases/{source.CaseId}/routing-decisions")).StatusCode);
    }

    [Fact]
    public async Task OutsideRequiresCurrentRealSourceWholeCaseAttestationAndKeepsAppendOnlyHistory()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var reporter = await User(UserRoleCode.Reporter);
        var route = await Route(32648, false);
        var source = await Intake(reporter.Id);
        await using var factory = Factory();
        using var client = await WebCookieTestClient.Login(factory, sup.Email!);
        await Coverage(client, route, sup.Id);
        var other = await Intake(reporter.Id);
        // Legacy multi-Report inbox fixture only; this does not grant Supervisor an HTTP merge action.
        await using (var legacy = sql.CreateDbContext())
        {
            await using var transaction = await legacy.Database.BeginTransactionAsync();
            var active = JsonSerializer.Serialize(new[] { source.ReportId, other.ReportId });
            var none = "[]";
            var now = DateTimeOffset.UtcNow;
            await legacy.Database.ExecuteSqlInterpolatedAsync($"UPDATE [IncidentCases] SET [ActiveReportIdsJson]={active},[Revision]=[Revision]+1 WHERE [Id]={source.CaseId}");
            await legacy.Database.ExecuteSqlInterpolatedAsync($"UPDATE [IncidentCases] SET [ActiveReportIdsJson]={none},[Status]={(byte)IncidentCaseStatus.Linked},[LinkedTargetCaseId]={source.CaseId},[Revision]=[Revision]+1 WHERE [Id]={other.CaseId}");
            await legacy.Set<HuyCaseReportLink>().Where(l => l.CaseId == other.CaseId && l.ReportId == other.ReportId && l.EndedAt == null)
                .ExecuteUpdateAsync(l => l.SetProperty(x => x.EndedAt, now));
            legacy.Add(new HuyCaseReportLink { Id = Guid.NewGuid(), CaseId = source.CaseId, ReportId = other.ReportId, StartedAt = now });
            await legacy.SaveChangesAsync(); await transaction.CommitAsync();
        }
        var candidate = await Candidate(client, source, source.EvidenceA, route.RouteId);
        var properties = candidate.GetProperty("route");
        var pin = Assert.Single(properties.GetProperty("coveragePins").EnumerateArray());
        var attestation = new
        {
            scopeReason = "Whole Case compared against the identified authoritative coverage boundaries",
            conflictResolution = "Reviewed all three original photographs; this attestation records the limited external location, without treating missing GPS as outside",
            temporalBasis = "SERVER_RECEIVED_AT",
            reports = new[] { new { reportId = source.ReportId, evidenceId = source.EvidenceA,
                locationReason = "Reviewed exact image and the versioned road scope",
                responsibilityReason = "Supervisor attests that the reported damage is outside the sourced coverage; this is not a machine distance decision",
                sourceReferences = new[] { new { kind = "ROUTE_VERSION", id = route.RouteId, version = properties.GetProperty("geometryHash").GetString() },
                    new { kind = "COVERAGE_MAPPING", id = pin.GetProperty("id").GetGuid(), version = pin.GetProperty("version").GetString() } } } }
        };
        var read = await Get(client, $"/api/v1/cases/{source.CaseId}");
        var version = read.GetProperty("version").GetString()!;
        var incomplete = await Write(client, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(),
            new { outcome = "OUTSIDE_COMPANY_SCOPE", reason = "One Report must not reject a multi-Report Case", reportId = source.ReportId, attestation });
        Assert.Equal(HttpStatusCode.Conflict, incomplete.StatusCode);
        Assert.Equal("routing_attestation_incomplete", (await incomplete.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        attestation = attestation with { reports = [attestation.reports[0], attestation.reports[0] with { reportId = other.ReportId, evidenceId = other.EvidenceA }] };
        var body = new { outcome = "OUTSIDE_COMPANY_SCOPE", reason = "Source-backed Supervisor intake attestation", reportId = source.ReportId, evidenceId = source.EvidenceA, attestation };
        var key = Guid.NewGuid().ToString();
        var result = await Write(client, source.CaseId, "routing-decisions", version, key, body);
        Assert.True(result.StatusCode == HttpStatusCode.OK, await result.Content.ReadAsStringAsync() + _logs.CombinedEntries);
        Assert.Equal(HttpStatusCode.OK, (await Write(client, source.CaseId, "routing-decisions", version, key, body)).StatusCode);
        var saved = await result.Content.ReadFromJsonAsync<JsonElement>();
        read = await Get(client, $"/api/v1/cases/{source.CaseId}");
        Assert.Equal("OUTSIDE_COMPANY_SCOPE", read.GetProperty("routingDisposition").GetProperty("outcome").GetString());
        Assert.Equal("UNASSIGNED", read.GetProperty("status").GetString());
        // A changed Defect lineage must invalidate the decision even if its obligation/mapping rows stay unchanged.
        await using (var changed = sql.CreateDbContext())
        {
            var defect = await changed.Defects.SingleAsync(d => d.RoadSectionVersionId == route.RouteId);
            await changed.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Defects] SET [RoadSectionVersionId]=NULL WHERE [Id]={defect.Id}");
            var invalidated = await Get(client, $"/api/v1/cases/{source.CaseId}");
            Assert.Equal("REVIEW_REQUIRED", invalidated.GetProperty("routingDisposition").GetProperty("state").GetString());
            Assert.Equal(JsonValueKind.Null, invalidated.GetProperty("routingDisposition").GetProperty("outcome").ValueKind);
            await changed.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Defects] SET [RoadSectionVersionId]={route.RouteId} WHERE [Id]={defect.Id}");
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Write(client, source.CaseId, "routing-decisions", version, key,
            new { outcome = "NEEDS_LOCATION_EVIDENCE", reason = "Changed payload", reportId = source.ReportId })).StatusCode);
        await using var proof = sql.CreateDbContext();
        var id = saved.GetProperty("decision").GetProperty("decisionId").GetGuid();
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => proof.Database.ExecuteSqlInterpolatedAsync($"UPDATE [CaseRoutingEvents] SET [Reason]='rewrite' WHERE [Id]={id}"));
        Assert.Equal(1, await proof.Set<CaseRoutingEvent>().CountAsync(e => e.CaseId == source.CaseId));
        var oldRoute = await proof.RoadSectionVersions.SingleAsync(v => v.Id == route.RouteId);
        oldRoute.ClearCurrent(); await proof.SaveChangesAsync();
        var currentRoute = RoadSectionVersion.Create(Guid.NewGuid(), oldRoute.RoadSectionId, 2, true,
            (LineString)oldRoute.Geometry.Copy(), DateTimeOffset.UtcNow, "A new confirmed geometry does not replace historical obligation pins");
        proof.Add(currentRoute); await proof.SaveChangesAsync();
        var mapFeatures = new List<JsonElement>();
        string? mapCursor = null;
        do
        {
            var map = await Get(client, "/api/v1/company/road-map?limit=100" +
                (mapCursor is null ? "" : "&cursor=" + Uri.EscapeDataString(mapCursor)));
            mapFeatures.AddRange(map.GetProperty("features").EnumerateArray().Select(f => f.Clone()));
            mapCursor = map.GetProperty("nextCursor").GetString();
        } while (mapCursor is not null);
        var historical = Assert.Single(mapFeatures, f => f.GetProperty("id").GetGuid() == route.RouteId);
        Assert.False(historical.GetProperty("properties").GetProperty("isCurrent").GetBoolean());
        Assert.Single(historical.GetProperty("properties").GetProperty("obligationGeometryPins").EnumerateArray());
        Assert.Contains(mapFeatures, f => f.GetProperty("id").GetGuid() == currentRoute.Id);
        var newCandidate = await Candidate(client, source, source.EvidenceA, currentRoute.Id);
        Assert.Empty(newCandidate.GetProperty("route").GetProperty("coveragePins").EnumerateArray());
        var stale = await Get(client, $"/api/v1/cases/{source.CaseId}");
        Assert.Equal("REVIEW_REQUIRED", stale.GetProperty("routingDisposition").GetProperty("state").GetString());
        Assert.Equal(JsonValueKind.Null, stale.GetProperty("routingDisposition").GetProperty("outcome").ValueKind);
    }

    [Fact]
    public async Task RoutingDecisionChecksCsrfRoleScopeStaleVersionAndSerializesConcurrentWriters()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager);
        var reporter = await User(UserRoleCode.Reporter);
        var source = await Intake(reporter.Id);
        var other = await Intake(reporter.Id);
        await using var factory = Factory();
        using var client = await WebCookieTestClient.Login(factory, sup.Email!);
        var read = await Get(client, $"/api/v1/cases/{source.CaseId}");
        var version = read.GetProperty("version").GetString()!;
        var body = new { outcome = "NEEDS_LOCATION_EVIDENCE", reason = "Current Case evidence lacks position", reportId = source.ReportId };
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await Write(client, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(), body)).StatusCode);
        await WebCookieTestClient.RefreshCsrf(client);
        Assert.Equal(HttpStatusCode.NotFound, (await Write(client, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(),
            new { body.outcome, body.reason, reportId = other.ReportId })).StatusCode);
        using var manager = await WebCookieTestClient.Login(factory, pm.Email!);
        Assert.Equal(HttpStatusCode.Forbidden, (await Write(manager, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(), body)).StatusCode);
        using var bearer = await Bearer(factory, sup.Email!);
        var results = await Task.WhenAll(Write(bearer, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(), body),
            Write(client, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(), body));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.PreconditionFailed);
        await using var db = sql.CreateDbContext();
        Assert.Equal(1, await db.Set<CaseRoutingEvent>().CountAsync(e => e.CaseId == source.CaseId));
        Assert.Equal(1, await db.AuditLogs.CountAsync(a => a.EntityId == source.CaseId && a.EventType == "case_routing_decided"));
    }

    [Fact]
    public async Task SupplementAndChangedLinksRespectCaseProjectScopeAndInvalidateOldEvaluations()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager);
        var outsider = await User(UserRoleCode.ProjectManager);
        var reporter = await User(UserRoleCode.Reporter);
        var source = await Intake(reporter.Id);
        var other = await Intake(reporter.Id);
        var route = await Route(32649, false, pm: pm.Id);
        Guid supplementEvidence;
        Guid supplement;
        await using (var db = sql.CreateDbContext())
        {
            var report = await db.Reports.Include(r => r.OriginalEvidence).Include(r => r.Supplements).SingleAsync(r => r.Id == source.ReportId);
            var original = report.OriginalEvidence[0];
            supplementEvidence = Guid.NewGuid(); supplement = Guid.NewGuid();
            report.AddSupplement(supplement, "Separately located supplement", [VerifiedEvidenceReference.Create(supplementEvidence,
                original.FileId, original.FileVersion, reporter.Id, EvidenceCaptureMetadata.Create(DateTimeOffset.UtcNow,
                    EvidenceLocationSource.Exif, 10m, 111m, 7m))], DateTimeOffset.UtcNow);
            db.Entry(report).Property<long>("Revision").CurrentValue++;
            await db.SaveChangesAsync();
        }
        await using var factory = Factory();
        using var client = await WebCookieTestClient.Login(factory, sup.Email!);
        var evaluation = await Get(client, CandidatePath(source, supplementEvidence));
        Assert.Equal(supplement, evaluation.GetProperty("source").GetProperty("supplementId").GetGuid());
        Assert.Equal(111m, evaluation.GetProperty("source").GetProperty("longitude").GetDecimal());
        var candidate = await Candidate(client, source, supplementEvidence, route.RouteId);
        Assert.InRange(candidate.GetProperty("distanceMeters").GetDouble(), 0, .01);
        var version = (await Get(client, $"/api/v1/cases/{source.CaseId}")).GetProperty("version").GetString()!;
        await Route(32648, false);
        Assert.Equal(HttpStatusCode.Conflict, (await Write(client, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(),
            new
            {
                outcome = "NEEDS_LOCATION_EVIDENCE",
                reason = "Old evaluation must not be silently repinned",
                reportId = source.ReportId,
                evidenceId = supplementEvidence,
                routingEvaluationVersion = evaluation.GetProperty("evaluationVersion").GetString()
            })).StatusCode);
        var triage = await Write(client, source.CaseId, "triage", version, Guid.NewGuid().ToString(),
            new { projectId = route.ProjectId, verificationMethod = "FIELD", reason = "Grounded scope pending verification" });
        Assert.True(triage.StatusCode == HttpStatusCode.OK, await triage.Content.ReadAsStringAsync() + _logs.CombinedEntries);
        using var manager = await WebCookieTestClient.Login(factory, pm.Email!);
        using var wrong = await WebCookieTestClient.Login(factory, outsider.Email!);
        var scoped = await Get(manager, CandidatePath(source, supplementEvidence) + "&limit=100");
        Assert.All(scoped.GetProperty("candidates").EnumerateArray(), c => Assert.Equal(route.ProjectId, c.GetProperty("route").GetProperty("projectId").GetGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, (await wrong.GetAsync(CandidatePath(source, supplementEvidence))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync($"/api/v1/cases/{source.CaseId}/routing-candidates?reportId={other.ReportId}&evidenceId={other.EvidenceA}")).StatusCode);
        await using (var db = sql.CreateDbContext())
            await db.ProjectMembers.Where(m => m.ProjectId == route.ProjectId && m.UserId == pm.Id)
                .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync(CandidatePath(source, supplementEvidence))).StatusCode);
        await using (var db = sql.CreateDbContext())
            await db.Set<HuyCaseReportLink>().Where(l => l.CaseId == source.CaseId && l.ReportId == source.ReportId && l.EndedAt == null)
                .ExecuteUpdateAsync(l => l.SetProperty(x => x.EndedAt, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(CandidatePath(source, supplementEvidence))).StatusCode);
    }

    private async Task<JsonElement> Candidate(HttpClient client, Source source, Guid evidence, Guid routeId)
    {
        string? cursor = null;
        do
        {
            var page = await Get(client, CandidatePath(source, evidence) + "&limit=100" + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)));
            foreach (var candidate in page.GetProperty("candidates").EnumerateArray())
                if (candidate.GetProperty("route").GetProperty("routeVersionId").GetGuid() == routeId) return candidate;
            cursor = page.GetProperty("nextCursor").GetString();
        } while (cursor is not null);
        throw new InvalidOperationException("Expected canonical route was missing from all routing pages.");
    }

    [Fact]
    public async Task TestOnlyCoverageIsVisibleAsProvenanceButCannotAuthorizeOutsideAttestation()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var reporter = await User(UserRoleCode.Reporter);
        var route = await Route(32648, false);
        var source = await Intake(reporter.Id);
        await using var factory = Factory();
        using var client = await WebCookieTestClient.Login(factory, sup.Email!);
        await Coverage(client, route, sup.Id, synthetic: true);
        var candidate = await Candidate(client, source, source.EvidenceA, route.RouteId);
        var properties = candidate.GetProperty("route");
        var coverage = Assert.Single(properties.GetProperty("coveragePins").EnumerateArray());
        Assert.Equal("TEST_ONLY", coverage.GetProperty("provenance").GetString());
        Assert.Equal("UNKNOWN", candidate.GetProperty("responsibility").GetString());
        Assert.Contains("TEST_ONLY_MAPPING", candidate.GetProperty("missingReasons").EnumerateArray().Select(r => r.GetString()));
        var version = (await Get(client, $"/api/v1/cases/{source.CaseId}")).GetProperty("version").GetString()!;
        var response = await Write(client, source.CaseId, "routing-decisions", version, Guid.NewGuid().ToString(), new
        {
            outcome = "OUTSIDE_COMPANY_SCOPE",
            reason = "Synthetic sources must never become authoritative",
            reportId = source.ReportId,
            attestation = new
            {
                scopeReason = "Explicit scope claim",
                conflictResolution = "Reviewed all current Reports",
                temporalBasis = "SERVER_RECEIVED_AT",
                reports = new[] { new { reportId = source.ReportId, evidenceId = source.EvidenceA,
                    locationReason = "Selected evidence location", responsibilityReason = "Cannot promote this source",
                    sourceReferences = new[] {
                        new { kind = "ROUTE_VERSION", id = route.RouteId, version = properties.GetProperty("geometryHash").GetString() },
                        new { kind = "COVERAGE_MAPPING", id = coverage.GetProperty("id").GetGuid(), version = coverage.GetProperty("version").GetString() } } } }
            }
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("routing_responsibility_unresolved", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = sql.CreateDbContext();
        Assert.False(await db.Set<CaseRoutingEvent>().AnyAsync(e => e.CaseId == source.CaseId));
    }

    private async Task Coverage(HttpClient supervisor, RouteIds route, Guid actor, bool synthetic = false)
    {
        await using var db = sql.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var version = await db.RoadSectionVersions.SingleAsync(r => r.Id == route.RouteId);
        var type = DefectType.Create("ROUTING-" + Guid.NewGuid().ToString("N"), "Isolated routing authority fixture");
        var defect = Defect.Create(Guid.NewGuid(), route.ProjectId, route.RouteId, null, type.Code, null,
            DefectSeverity.Low, DefectStatus.Open, new Point(500000, 1105412.4913010783) { SRID = 32648 }, now);
        db.AddRange(type, defect); await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), route.ProjectId, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), version.RoadSectionId, "ROUTING_ISOLATED_FRAME", "Isolated authority fixture", 1, 20, 0, 2));
        db.Add(RepairPackage.Create(Guid.NewGuid(), route.ProjectId, defect.Id, [obligation])); await db.SaveChangesAsync();
        var purpose = synthetic ? "TEST_ONLY_COVERAGE" : "DOCUMENT";
        var file = StoredFile.Create(Guid.NewGuid(), (synthetic ? "TEST_ONLY/" : "isolated-routing-authority/") + Guid.NewGuid(), "source.pdf", "application/pdf", 4, new string('b', 64), actor, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor, file.StorageUri, purpose, file.MimeType, 4, file.Checksum, 8388608, now.AddHours(1));
        upload.StartUploading("isolated-routing", now); db.AddRange(file, upload, FileScope.Create(Guid.NewGuid(), file.Id, route.ProjectId, route.ProjectId, actor, purpose, now));
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var document = HandoverDocument.Create(Guid.NewGuid(), route.ProjectId, "ROUTING-AUTHORITY", DateOnly.FromDateTime(now.UtcDateTime.AddDays(-2)), actor, file.Id, "Isolated source fixture; not proof of a deployed real document");
        db.Add(document); await db.SaveChangesAsync();
        var path = $"/api/v1/projects/{route.ProjectId}/road-coverage";
        var read = await Get(supervisor, path);
        var handover = Assert.Single(read.GetProperty("sources").EnumerateArray(), s => s.GetProperty("kind").GetString() == "HANDOVER");
        var maintenance = Assert.Single(read.GetProperty("sources").EnumerateArray(), s => s.GetProperty("kind").GetString() == "MAINTENANCE_BASIS");
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new
            {
                scopeObligationId = obligation.Id,
                expectedScopeHash = read.GetProperty("scopes")[0].GetProperty("scopeHash").GetString(),
                handoverDocumentId = document.Id,
                handoverVersion = handover.GetProperty("version").GetString(),
                sourceKind = "MAINTENANCE_BASIS",
                sourceId = file.Id,
                sourceVersion = maintenance.GetProperty("version").GetString(),
                applicableFromUtc = now.AddDays(-1),
                applicableToUtc = now.AddDays(1),
                provenance = synthetic ? "TEST_ONLY" : "REAL_SOURCE",
                reason = "Isolated authoritative input fixture for routing HTTP checks"
            })
        };
        request.Headers.TryAddWithoutValidation("If-Match", '"' + read.GetProperty("version").GetString() + '"');
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await supervisor.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync() + _logs.CombinedEntries);
    }

    private sealed record Source(Guid CaseId, Guid ReportId, Guid EvidenceA, Guid EvidenceB, Guid MissingEvidence, Guid FileA);
    private sealed record RouteIds(Guid ProjectId, Guid RouteId);
    private async Task<(int Outbox, int Defects, int FieldTasks, int ProcessingJobs)> WorkCounts()
    {
        await using var db = sql.CreateDbContext();
        return (await db.OutboxMessages.CountAsync(), await db.Defects.CountAsync(),
            await db.FieldInspectionTasks.CountAsync(), await db.ProcessingJobs.CountAsync());
    }
    private Task<ApplicationUser> User(UserRoleCode role) => sql.CreateUserAsync("ROUTING-" + Guid.NewGuid().ToString("N"), "Current1!", role);
    private async Task<RouteIds> Route(int srid, bool closed, bool native = false, Guid? pm = null)
    {
        await using var db = sql.CreateDbContext();
        var project = Project.Create(Guid.NewGuid(), "MAP-" + Guid.NewGuid().ToString("N"), "Routing map", null, srid, null, null, DateTimeOffset.UtcNow);
        if (closed) project.Status = ProjectStatus.Closed;
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, "R1", "Road map test");
        var shape = new LineString([new Coordinate(499000, 1105412.4913010783), new Coordinate(501000, 1105412.4913010783)]) { SRID = srid };
        var route = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, shape, DateTimeOffset.UtcNow, "Reference geometry");
        db.AddRange(project, section);
        if (native)
        {
            var profile = new CrsProfileRevision
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Code = "NATIVE",
                Revision = 1,
                SourceSrid = srid,
                SampleOnly = true,
                Status = "CANDIDATE",
                CreatedAt = DateTimeOffset.UtcNow,
                PayloadJson = "{}",
                CreatedBy = await db.Users.Where(u => u.RoleCode == UserRoleCode.Supervisor).Select(u => u.Id).FirstAsync()
            };
            route.PinNativeProfile(profile.Id); db.Add(profile);
        }
        if (pm is Guid manager) db.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, manager, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));
        db.Add(route); await db.SaveChangesAsync();
        return new(project.Id, route.Id);
    }
    private async Task<Source> Intake(Guid owner)
    {
        await using var db = sql.CreateDbContext();
        var references = new List<VerifiedEvidenceReference>();
        var files = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var file = StoredFile.Create(Guid.NewGuid(), "routing/" + Guid.NewGuid().ToString("N"), "image.jpg", "image/jpeg", 4, new string('a', 64), owner, DateTimeOffset.UtcNow, null);
            var upload = UploadSession.Create(Guid.NewGuid(), file.Id, owner, file.StorageUri, "REPORT_PHOTO", "image/jpeg", 4, file.Checksum, 8 * 1024 * 1024, DateTimeOffset.UtcNow.AddHours(1));
            upload.StartUploading("routing-test", DateTimeOffset.UtcNow); db.AddRange(file, upload, FileScope.CreatePrivate(Guid.NewGuid(), file.Id, owner, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), DateTimeOffset.UtcNow); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
            references.Add(VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, Convert.ToBase64String(upload.RowVersion), owner,
                i == 2 ? EvidenceCaptureMetadata.Create(null, EvidenceLocationSource.Unknown) : EvidenceCaptureMetadata.Create(DateTimeOffset.UtcNow.AddDays(-5),
                    i == 0 ? EvidenceLocationSource.Capture : EvidenceLocationSource.Manual, 10m, 105m + i)));
            files.Add(file.Id);
        }
        var report = Report.Create(Guid.NewGuid(), owner, "Routing source", DateTimeOffset.UtcNow, references);
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, DateTimeOffset.UtcNow);
        db.AddRange(report, incident, new HuyCaseReportLink { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = DateTimeOffset.UtcNow });
        db.Entry(report).Property<long>("Revision").CurrentValue = 1; db.Entry(incident).Property<long>("Revision").CurrentValue = 1;
        await db.SaveChangesAsync();
        return new(incident.Id, report.Id, references[0].EvidenceId, references[1].EvidenceId, references[2].EvidenceId, files[0]);
    }
    private static string CandidatePath(Source source, Guid evidence) => $"/api/v1/cases/{source.CaseId}/routing-candidates?reportId={source.ReportId}&evidenceId={evidence}";
    private async Task<JsonElement> Get(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()} {_logs.CombinedEntries}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var destination = Environment.GetEnvironmentVariable("RG_ROUTING_HTTP_EVIDENCE");
        if (destination is not null)
            await File.AppendAllTextAsync(destination, JsonSerializer.Serialize(new
            {
                path,
                status = (int)response.StatusCode,
                etag = response.Headers.ETag?.Tag,
                body
            }) + Environment.NewLine);
        return body;
    }
    private static Task<HttpResponseMessage> Write(HttpClient client, Guid id, string action, string version, string key, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{id}/{action}") { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\""); request.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(request);
    }
    private static async Task<HttpClient> Bearer(AuthenticationWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }
    private sealed class JsonElementComparer : IEqualityComparer<JsonElement>
    {
        internal static readonly JsonElementComparer Instance = new();
        public bool Equals(JsonElement x, JsonElement y) => x.GetRawText() == y.GetRawText();
        public int GetHashCode(JsonElement value) => value.GetRawText().GetHashCode(StringComparison.Ordinal);
    }
}
