using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

public sealed partial class RoutingMapRepository
{
    public async Task GuardDecisionAsync(Guid actor, UserRoleCode role, Guid caseId, CaseRoutingDecisionInput input,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token)
    {
        if (role != UserRoleCode.Supervisor) Fail(403, "access_forbidden");
        await GuardAsync(actor, role, caseId, projectAccess, token);
        await EvidenceAsync(caseId, input.ReportId, input.EvidenceId, token);
    }
    public Task<RoutingDecisionPage> HistoryAsync(Guid actor, UserRoleCode role, Guid caseId, int limit,
        string? cursor, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token)
        => InReadAsync<RoutingDecisionPage>(async () =>
        {
            if (role != UserRoleCode.Supervisor) Fail(403, "access_forbidden");
            await GuardAsync(actor, role, caseId, projectAccess, token);
            CheckLimit(limit);
            var latest = await LatestAsync(caseId, token);
            var registry = await RegistryVersionAsync(token);
            var source = await CaseSourceVersionAsync(caseId, token);
            var snapshot = Hash(new { latest?.Id, registry, source });
            var filter = Hash(new { caseId, limit });
            var after = DecodeCursor(cursor, actor, "history", filter, snapshot);
            var rows = await db.Set<CaseRoutingEvent>().AsNoTracking().Where(x => x.CaseId == caseId && x.Kind == "DECISION" &&
                (after == null || x.Id.CompareTo(after.Value) > 0)).OrderBy(x => x.Id).Take(limit + 1).ToArrayAsync(token);
            return new(rows.Take(limit).Select(x => View(x, latest?.Id == x.Id
                ? x.CaseSourceVersion == source && x.RegistryVersion == registry ? "EFFECTIVE" : "REVIEW_REQUIRED"
                : "SUPERSEDED")).ToArray(), rows.Length > limit
                    ? EncodeCursor(actor, "history", filter, snapshot, rows[limit - 1].Id) : null);
        }, token);

    public async Task<RoutingWriteView> DecideAsync(Guid actor, UserRoleCode role, Guid caseId, string expectedVersion,
        CaseRoutingDecisionInput input, Guid? correlation, Func<Guid, CancellationToken, Task<bool>> projectAccess,
        CancellationToken token)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Routing decisions require the receipt transaction.");
        if (role != UserRoleCode.Supervisor) Fail(403, "access_forbidden");
        await GuardAsync(actor, role, caseId, projectAccess, token);
        var incident = await db.IncidentCases.SingleAsync(c => c.Id == caseId, token);
        if (incident.ProjectId is not null || incident.Status != IncidentCaseStatus.Unassigned) Fail(409, "case_already_assigned");
        if (Convert.ToBase64String(db.Entry(incident).Property<byte[]>("RowVersion").CurrentValue!) != expectedVersion)
            Fail(412, "concurrency_conflict");
        if (input.Outcome is not ("NEEDS_LOCATION_EVIDENCE" or "OUTSIDE_COMPANY_SCOPE") ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 2000 || input.ReportId == Guid.Empty)
            Fail(400, "validation_error");
        var source = await EvidenceAsync(caseId, input.ReportId, input.EvidenceId, token);
        var registry = await RegistryVersionAsync(token);
        if (input.RoutingEvaluationVersion is not null && input.RoutingEvaluationVersion != Evaluation(source, registry, null))
            Fail(409, "routing_evaluation_stale");
        if (input.Outcome == "OUTSIDE_COMPANY_SCOPE") await AttestationAsync(caseId, input.Attestation, token);
        var previous = await LatestAsync(caseId, token);
        var entry = new CaseRoutingEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Sequence = (previous?.Sequence ?? 0) + 1,
            Outcome = input.Outcome,
            Reason = input.Reason.Trim(),
            ActorId = actor,
            At = DateTimeOffset.UtcNow,
            ReportId = input.ReportId,
            EvidenceId = input.EvidenceId,
            CaseSourceVersion = source.CaseSourceVersion,
            EvaluationVersion = input.RoutingEvaluationVersion,
            RegistryVersion = registry,
            SupersedesId = previous?.Id,
            SourceSnapshotJson = JsonSerializer.Serialize(new RoutingDecisionSourceSnapshot(source, input.Attestation), Json)
        };
        db.Add(entry);
        db.Entry(incident).Property<long>("Revision").CurrentValue++;
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, entry.At, "case_routing_decided", "IncidentCase", caseId,
            null, JsonSerializer.Serialize(new { decisionId = entry.Id, entry.Outcome }), entry.Reason,
            "case.routing.decision.v1", correlation, ["decisionId", "outcome"]));
        await db.SaveChangesAsync(token);
        return new(View(entry, "EFFECTIVE"), Convert.ToBase64String(db.Entry(incident).Property<byte[]>("RowVersion").CurrentValue!));
    }

    private async Task AttestationAsync(Guid caseId, RoutingWholeCaseAttestation? attestation, CancellationToken token)
    {
        if (attestation is null || !Text(attestation.ScopeReason) || !Text(attestation.ConflictResolution) ||
            attestation.TemporalBasis != "SERVER_RECEIVED_AT" || attestation.Reports is not { Count: > 0 and <= 100 } ||
            attestation.Reports.Any(r => r is null))
            Fail(409, "routing_attestation_incomplete");
        var reports = await db.Set<HuyCaseReportLink>().Where(l => l.CaseId == caseId && l.EndedAt == null)
            .Select(l => l.ReportId).ToArrayAsync(token);
        if (!reports.Order().SequenceEqual(attestation!.Reports.Select(r => r.ReportId).Order())) Fail(409, "routing_attestation_incomplete");
        foreach (var report in attestation.Reports)
        {
            if (!Text(report.LocationReason) || !Text(report.ResponsibilityReason) || report.SourceReferences is not { Count: > 0 and <= 20 })
                Fail(409, "routing_attestation_incomplete");
            var evidence = await EvidenceAsync(caseId, report.ReportId, report.EvidenceId, token);
            var routeIds = new List<Guid>();
            var mappings = new List<RoadCoverageMapping>();
            foreach (var reference in report.SourceReferences)
            {
                if (reference is null) throw new CaseWorkflowException(400, "validation_error");
                if (reference.Kind == "ROUTE_VERSION")
                {
                    var head = await (from route in db.RoadSectionVersions.AsNoTracking()
                                      join section in db.RoadSections.AsNoTracking() on route.RoadSectionId equals section.Id
                                      join project in db.Projects.AsNoTracking() on section.ProjectId equals project.Id
                                      where route.Id == reference.Id
                                      select new RouteHead(route.Id, project.Id, project.Name, project.Status, section.Id,
                                          section.Code, section.Name, route.VersionNo, route.IsCurrent, route.EffectiveFrom,
                                          route.CrsProfileRevisionId, route.Geometry.NumPoints)).SingleOrDefaultAsync(token);
                    if (head is null || (await PropertiesAsync(head, token)).GeometryHash != reference.Version) Fail(409, "routing_source_stale");
                    if (head!.Points > MaxVertices || head.ProfileId is not null) Fail(409, "routing_location_unresolved");
                    var line = await db.RoadSectionVersions.Where(v => v.Id == head.Id).Select(v => v.Geometry).SingleAsync(token);
                    if (GeometryReason(head, line) is not null) Fail(409, "routing_location_unresolved");
                    routeIds.Add(head.Id);
                }
                else if (reference.Kind == "COVERAGE_MAPPING")
                {
                    var mapping = await db.Set<RoadCoverageMapping>().AsNoTracking().SingleOrDefaultAsync(m => m.Id == reference.Id, token);
                    if (mapping is null || mapping.Provenance != "REAL_SOURCE" || Hash(mapping) != reference.Version ||
                        await db.Set<RoadCoverageMapping>().AnyAsync(m => m.SupersedesId == reference.Id, token) ||
                        !await RoadCoverageResolver.CurrentSources(db, mapping, token) ||
                        evidence.ReceivedAt < mapping.ApplicableFromUtc || evidence.ReceivedAt >= mapping.ApplicableToUtc)
                        Fail(409, "routing_responsibility_unresolved");
                    // This is a human, source-backed whole-Case attestation, not an automatic inference
                    // from distance, top-N, mapping expiry or a missing warranty.
                    mappings.Add(mapping!);
                }
                else Fail(400, "validation_error");
            }
            if (routeIds.Count == 0 || mappings.Count == 0) Fail(409, "routing_responsibility_unresolved");
            foreach (var mapping in mappings)
                if (!await (from obligation in db.Set<RepairObligation>().AsNoTracking()
                            join defect in db.Defects.AsNoTracking() on obligation.DefectId equals defect.Id
                            join route in db.RoadSectionVersions.AsNoTracking() on defect.RoadSectionVersionId equals route.Id
                            where obligation.Id == mapping.ScopeObligationId && routeIds.Contains(route.Id) &&
                                route.RoadSectionId == mapping.RoadSectionId && defect.ProjectId == mapping.ProjectId
                            select obligation.Id).AnyAsync(token)) Fail(409, "reference_scope_conflict");
        }
    }

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 2000;
    private Task<CaseRoutingEvent?> LatestAsync(Guid caseId, CancellationToken token)
        => db.Set<CaseRoutingEvent>().AsNoTracking().Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.Sequence).FirstOrDefaultAsync(token);
    private static RoutingDecisionView View(CaseRoutingEvent row, string state)
    {
        var snapshot = row.SourceSnapshotJson is null ? null : JsonSerializer.Deserialize<RoutingDecisionSourceSnapshot>(row.SourceSnapshotJson, Json);
        return new(row.Id, row.Outcome!, row.Reason, row.ActorId, row.At, row.ReportId, row.EvidenceId,
            row.CaseSourceVersion, row.EvaluationVersion, row.TemporalBasis, row.SupersedesId, state,
            snapshot?.Attestation, snapshot?.Evidence);
    }

    public async Task ValidateTriageAsync(Guid actor, UserRoleCode role, CaseCommand command, CancellationToken token)
    {
        if (command.RoutingSource is not { } pin) return;
        var source = await EvidenceAsync(command.CaseId, pin.ReportId, pin.EvidenceId, token);
        if (pin.RoadSectionId is Guid section && !await db.RoadSectionVersions.AnyAsync(v => v.Id == command.RouteVersionId && v.RoadSectionId == section &&
            db.RoadSections.Any(s => s.Id == section && s.ProjectId == command.ProjectId), token)) Fail(409, "reference_scope_conflict");
        if (pin.RoutingEvaluationVersion is not null)
        {
            var registry = await RegistryVersionAsync(token);
            if (pin.RoutingEvaluationVersion != Evaluation(source, registry, role == UserRoleCode.ProjectManager ? command.ProjectId : null))
                Fail(409, "routing_evaluation_stale");
            if (command.RouteVersionId is null || command.SegmentSetId is null) Fail(400, "validation_error");
        }
    }

    public async Task RecordTriageAsync(Guid actor, CaseCommand command, CancellationToken token)
    {
        var previous = await LatestAsync(command.CaseId, token);
        if (previous is null && command.RoutingSource is null) return;
        var report = command.RoutingSource?.ReportId ?? previous!.ReportId;
        db.Add(new CaseRoutingEvent
        {
            Id = Guid.NewGuid(),
            CaseId = command.CaseId,
            Sequence = (previous?.Sequence ?? 0) + 1,
            Kind = "TRIAGE",
            Outcome = null,
            Reason = command.Reason,
            ActorId = actor,
            At = DateTimeOffset.UtcNow,
            ReportId = report,
            EvidenceId = command.RoutingSource?.EvidenceId,
            CaseSourceVersion = await CaseSourceVersionAsync(command.CaseId, token),
            RegistryVersion = await RegistryVersionAsync(token),
            SupersedesId = previous?.Id,
            EvaluationVersion = command.RoutingSource?.RoutingEvaluationVersion,
            TriageSourceJson = command.RoutingSource is null ? null : JsonSerializer.Serialize(command.RoutingSource, Json)
        });
    }

}
