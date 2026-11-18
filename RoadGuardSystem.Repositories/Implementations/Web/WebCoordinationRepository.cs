using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Web;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Web;

namespace RoadGuardSystem.Repositories.Implementations.Web;

public sealed class WebCoordinationRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider clock) : IWebCoordinationRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<TaskAssignee[]> TaskAssigneesAsync(Guid project, Guid[] tasks, CancellationToken token)
    {
        var survey = await (from assignment in db.SurveyAssignments.AsNoTracking()
                            join task in db.SurveyRequests.AsNoTracking() on assignment.SurveyRequestId equals task.Id
                            join user in db.Users.AsNoTracking() on assignment.OperatorUserId equals user.Id into users
                            from user in users.DefaultIfEmpty()
                            where task.ProjectId == project && tasks.Contains(task.Id)
                            orderby task.Id, assignment.AssignedAt, assignment.Id
                            select new TaskAssignee(task.Id, "SURVEY", assignment.Id, assignment.OperatorUserId,
                                user == null ? null : user.DisplayName, user == null ? "MISSING_ACCOUNT" : "CURRENT_ACCOUNT",
                                assignment.AssignedAt, assignment.EndedAt, assignment.EndedAt == null && assignment.RejectedAt == null)).ToArrayAsync(token);
        var field = await (from assignment in db.FieldInspectionAssignments.AsNoTracking()
                           join task in db.FieldInspectionTasks.AsNoTracking() on assignment.FieldInspectionTaskId equals task.Id
                           join user in db.Users.AsNoTracking() on assignment.AssignedToUserId equals user.Id into users
                           from user in users.DefaultIfEmpty()
                           where task.ProjectId == project && tasks.Contains(task.Id)
                           orderby task.Id, assignment.AssignedAt, assignment.Id
                           select new TaskAssignee(task.Id, "FIELD", assignment.Id, assignment.AssignedToUserId,
                               user == null ? null : user.DisplayName, user == null ? "MISSING_ACCOUNT" : "CURRENT_ACCOUNT",
                               assignment.AssignedAt, assignment.EndedAt, assignment.EndedAt == null && assignment.Status == FieldInspectionAssignmentStatus.Active)).ToArrayAsync(token);
        return survey.Concat(field).ToArray();
    }
    public async Task<OrganizationProfile?> OrganizationAsync(Guid user, CancellationToken token)
    {
        var row = await db.Users.AsNoTracking().SingleOrDefaultAsync(row => row.Id == user, token);
        return row is null ? null : new(row.Id, row.Organization, Convert.ToBase64String(row.RowVersion));
    }
    public async Task<bool> AuthorizeAsync(Guid actor, UserRoleCode role, Guid[] projects, CancellationToken token)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) ||
            !await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, token)) return false;
        if (role == UserRoleCode.Supervisor) return true;
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var allowed = await db.ProjectMembers.AsNoTracking().Where(row => projects.Contains(row.ProjectId) && row.UserId == actor &&
            row.RoleCode == role && row.Status == ProjectMemberStatus.Active && row.ValidFrom <= day &&
            (row.ValidTo == null || row.ValidTo >= day)).Select(row => row.ProjectId).Distinct().ToArrayAsync(token);
        return projects.All(allowed.Contains);
    }

    public async Task<ProjectOverview[]> OverviewsAsync(Guid[] ids, CancellationToken token)
    {
        var projects = await db.Projects.AsNoTracking().Where(row => ids.Contains(row.Id)).OrderBy(row => row.Id).ToArrayAsync(token);
        var routes = await (from version in db.RoadSectionVersions.AsNoTracking()
                            join road in db.RoadSections.AsNoTracking() on version.RoadSectionId equals road.Id
                            where ids.Contains(road.ProjectId) && version.IsCurrent
                            select new { road.ProjectId, Version = version }).ToArrayAsync(token);
        var routeIds = routes.Select(row => row.Version.Id).ToArray();
        var native = await db.Set<NativeRouteVersionFacts>().AsNoTracking().Where(row => routeIds.Contains(row.RoadSectionVersionId))
            .ToDictionaryAsync(row => row.RoadSectionVersionId, token);
        var warranties = await db.Warranties.AsNoTracking().Where(row => ids.Contains(row.ProjectId)).OrderBy(row => row.Id).ToArrayAsync(token);
        var counts = await db.Defects.AsNoTracking().Where(row => row.ProjectId.HasValue && ids.Contains(row.ProjectId.Value))
            .GroupBy(row => new { row.ProjectId, row.Status }).Select(group => new { group.Key.ProjectId, group.Key.Status, Count = group.LongCount() }).ToArrayAsync(token);
        return projects.Select(project =>
        {
            var lines = routes.Where(row => row.ProjectId == project.Id).Select(row =>
            {
                var source = row.Version; native.TryGetValue(source.Id, out var facts);
                double? length = facts?.CanonicalLengthMeters ??
                    (source.Geometry is { IsEmpty: false, SRID: 32648 or 32649 } ? source.Geometry.Length : null);
                return new ProjectRouteOverview(source.RoadSectionId, source.Id, source.VersionNo, source.Geometry.SRID, source.CrsProfileRevisionId, length);
            }).OrderBy(row => row.RoadSectionId).ToArray();
            var unknown = counts.Where(row => row.ProjectId == project.Id && row.Status is not (DefectStatus.Open or DefectStatus.Verified or DefectStatus.Rejected or DefectStatus.Closed)).Sum(row => row.Count);
            var open = counts.Where(row => row.ProjectId == project.Id && row.Status is DefectStatus.Open or DefectStatus.Verified).Sum(row => row.Count);
            return new ProjectOverview(project.Id, project.Name, project.Locality, project.EngineeringUtmSrid, lines,
                lines.Length > 0 && lines.All(row => row.LengthMeters.HasValue) ? lines.Sum(row => row.LengthMeters!.Value) : null,
                lines.Length == 0 ? "MISSING" : lines.Any(row => row.LengthMeters is null) ? "INCOMPLETE" : "AVAILABLE",
                warranties.Where(row => row.ProjectId == project.Id).Select(row => new WarrantyOverview(row.Id, row.RoadSectionId,
                    row.HandoverDate, row.WarrantyStartDate, row.WarrantyEndDate, row.Status.ToString().ToUpperInvariant())).ToArray(),
                new("defect.open.v1", ["OPEN", "VERIFIED"], unknown == 0 ? open : null, unknown == 0 ? "AVAILABLE" : "INCOMPLETE", unknown),
                Convert.ToBase64String(project.RowVersion));
        }).ToArray();
    }

    public async Task<ProjectContact[]> ContactsAsync(Guid project, Guid? after, int take, CancellationToken token)
    {
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var rows = await (from member in db.ProjectMembers.AsNoTracking()
                          join user in db.Users.AsNoTracking() on member.UserId equals user.Id
                          where member.ProjectId == project && member.Status == ProjectMemberStatus.Active && user.RoleCode == member.RoleCode && user.RoleCode != UserRoleCode.Reporter &&
                              member.ValidFrom <= day && (member.ValidTo == null || member.ValidTo >= day) &&
                              (!after.HasValue || member.Id.CompareTo(after.Value) > 0)
                          orderby member.Id
                          select new { Member = member, User = user }).Take(take).ToArrayAsync(token);
        return rows.Select(row => new ProjectContact(row.Member.Id, row.User.Id, row.User.DisplayName,
            row.Member.RoleCode.ToDbCode(), row.User.Email, row.User.PhoneNumber, row.User.PhoneNumberConfirmed, row.User.Organization)).ToArray();
    }

    public async Task<PolicyDiscovery[]> PoliciesAsync(Guid project, string kind, Guid? after, int take,
        string? defectType, bool applicableOnly, CancellationToken token)
    {
        if (kind == "DRAFT")
        {
            if (applicableOnly) return [];
            var drafts = await db.Set<RepairPolicyDraft>().AsNoTracking().Include(row => row.Changes).Where(row =>
                row.ProjectId == project && (!after.HasValue || row.Id.CompareTo(after.Value) > 0) &&
                (defectType == null || row.Changes.Any(change => change.Id == row.CurrentChangeId && change.DefectTypeCode == defectType)))
                .OrderBy(row => row.Id).Take(take).ToArrayAsync(token);
            var versions = await db.Set<RepairPolicyDraft>().Where(row => drafts.Select(draft => draft.Id).Contains(row.Id))
                .Select(row => new { row.Id, Version = EF.Property<byte[]>(row, "RowVersion") }).ToDictionaryAsync(row => row.Id, token);
            return drafts.Select(row =>
            {
                var head = row.Changes.Single(change => change.Id == row.CurrentChangeId);
                return new PolicyDiscovery(row.Id, kind, row.PublishedRevisionId is null ? "DRAFT" : "PUBLISHED", head.DefectTypeCode,
                    head.ChecklistVersion, null, head.At, false, Convert.ToBase64String(versions[row.Id].Version));
            }).ToArray();
        }
        var revisions = await db.Set<RepairPolicyRevision>().AsNoTracking().Include(row => row.Revocations)
            .Include(row => row.Measurements).Where(row => row.ProjectId == project &&
                (!after.HasValue || row.Id.CompareTo(after.Value) > 0) && (defectType == null || row.DefectTypeCode == defectType) &&
                (!applicableOnly || !row.Revocations.Any())).OrderBy(row => row.Id).Take(take).ToArrayAsync(token);
        var revisionIds = revisions.Select(row => row.Id).ToArray();
        var audits = await db.AuditLogs.AsNoTracking().Where(row => revisionIds.Contains(row.EntityId) &&
            row.EntityType == "RepairPolicy" && row.EventType == "repair_policy_publish" && row.Source == "h4.policy.v1" && row.CorrelationId == row.EntityId)
            .ToDictionaryAsync(row => row.EntityId, token);
        return revisions.Select(row =>
        {
            var view = RoadGuardSystem.Repositories.Implementations.Repairs.RepairPolicyRepository.RevisionView(row,
                audits.GetValueOrDefault(row.Id)?.Reason);
            return new PolicyDiscovery(row.Id, kind, view.State, row.DefectTypeCode, row.ChecklistVersion, row.Revision,
                row.PublishedAt, !row.IsRevoked, view.Version);
        }).ToArray();
    }

    private async Task<T> ReadTransactionAsync<T>(Func<Task<T>> read, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is not null) return await read();
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var value = await read(); await transaction.CommitAsync(token); return value;
        });
    }
    public Task<PackagePlanning?> PackageAsync(Guid project, Guid package, CancellationToken token)
        => ReadTransactionAsync(() => PackageCoreAsync(project, package, token), token);
    public Task<ItemPlanning?> ItemAsync(Guid project, Guid package, Guid item, CancellationToken token)
        => ReadTransactionAsync(() => ItemCoreAsync(project, package, item, token), token);
    public Task<DefectSourceReview?> DefectAsync(Guid project, Guid package, Guid item, CancellationToken token)
        => ReadTransactionAsync(() => DefectCoreAsync(project, package, item, token), token);
    private async Task<PackagePlanning?> PackageCoreAsync(Guid project, Guid package, CancellationToken token)
    {
        var row = await db.RepairPackages.AsNoTracking().Include(row => row.Obligations).SingleOrDefaultAsync(row => row.Id == package && row.ProjectId == project, token);
        if (row is null) return null;
        foreach (var obligation in row.Obligations)
            if (await ObligationResponsibilityScope.ResolveAsync(db, obligation.Id, obligation.ProjectId, token) != project) return null;
        var audit = await db.AuditLogs.AsNoTracking().SingleOrDefaultAsync(a => a.EntityId == package &&
            a.EntityType == "RepairPackage" && a.EventType == "repair_package_created", token);
        return new(row.Id, row.Name, row.Description, row.PlannedDueAt, audit?.ActorUserId, audit?.OccurredAtUtc,
            await VersionAsync(row, token));
    }
    private async Task<ItemPlanning?> ItemCoreAsync(Guid project, Guid package, Guid item, CancellationToken token)
    {
        var row = await db.RepairItems.AsNoTracking().SingleOrDefaultAsync(row => row.Id == item &&
            EF.Property<Guid?>(row, "PackageId") == package && row.ProjectId == project, token);
        if (row is null || await ObligationResponsibilityScope.ResolveAsync(db, row.ObligationId, row.ProjectId, token) != project) return null;
        return new(row.Id, row.PlannedQuantity, row.PlannedUnit, row.PlannedMaterials, await VersionAsync(row, token));
    }
    private async Task<DefectSourceReview?> DefectCoreAsync(Guid project, Guid package, Guid item, CancellationToken token)
    {
        var source = await db.RepairItems.AsNoTracking().Where(row => row.Id == item && EF.Property<Guid?>(row, "PackageId") == package)
            .Select(row => new { row.DefectId, row.ProjectId, row.ObligationId }).SingleOrDefaultAsync(token);
        if (source is null || source.ProjectId != project || await ObligationResponsibilityScope.ResolveAsync(db, source.ObligationId, source.ProjectId, token) != project) return null;
        var defect = await db.Defects.AsNoTracking().SingleOrDefaultAsync(row => row.Id == source.DefectId && row.ProjectId == project, token);
        if (defect is null) return null;
        var links = await db.Set<HuyDefectSourceLink>().AsNoTracking().Where(row => row.DefectId == defect.Id && row.ProjectId == project && row.EndedAt == null)
            .OrderBy(row => row.Id).Select(row => new DefectOriginReference(row.SourceKind.ToString(), row.SourceId, null)).ToArrayAsync(token);
        var reportIds = links.Where(row => row.Kind == "Report").Select(row => row.Id).ToArray();
        var cases = await (from link in db.Set<HuyCaseReportLink>().AsNoTracking()
                           join incident in db.IncidentCases.AsNoTracking() on link.CaseId equals incident.Id
                           where reportIds.Contains(link.ReportId) && link.EndedAt == null && incident.ProjectId == project
                           select new { link.ReportId, link.CaseId }).ToArrayAsync(token);
        links = links.Select(row => row with { CaseId = row.Kind == "Report" ? cases.SingleOrDefault(link => link.ReportId == row.Id)?.CaseId : null }).ToArray();
        return new(defect.Id, defect.DefectTypeCode, defect.Severity.ToString().ToUpperInvariant(), defect.Status.ToString().ToUpperInvariant(),
            defect.Geometry?.AsText(), defect.Geometry?.SRID, "CURRENT_SOURCE", await VersionAsync(defect, token), links,
            []);
    }
    private async Task<string> VersionAsync<TEntity>(TEntity row, CancellationToken token) where TEntity : class
    {
        var id = (Guid)db.Entry(row).Property("Id").CurrentValue!;
        var value = await db.Set<TEntity>().Where(entity => EF.Property<Guid>(entity, "Id") == id)
            .Select(entity => EF.Property<byte[]>(entity, "RowVersion")).SingleAsync(token);
        return Convert.ToBase64String(value);
    }

    public async Task<WebMetadataResult> WriteAsync(Guid actor, UserRoleCode role, WebMetadataCommand command, CancellationToken token)
    {
        try
        {
            var result = await receipts.ExecuteSerializableAsync(actor, command.ProjectId, "web.metadata." + command.Action,
                command.Key, Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(command, Json))).ToLowerInvariant(), async ct =>
                {
                    await GuardWrite(ct);
                    if (await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor && row.ProjectId == command.ProjectId &&
                        row.Operation == "web.metadata." + command.Action && row.IdempotencyKey == command.Key, ct)) throw new MetadataReceiptExists();
                    object target = command.Action switch
                    {
                        "locality" => await db.Projects.SingleAsync(row => row.Id == command.TargetId, ct),
                        "organization" => await db.Users.SingleAsync(row => row.Id == command.TargetId, ct),
                        "package" => await db.RepairPackages.SingleAsync(row => row.Id == command.TargetId && row.ProjectId == command.ProjectId, ct),
                        "item" => await db.RepairItems.SingleAsync(row => row.Id == command.TargetId && row.ProjectId == command.ProjectId &&
                            EF.Property<Guid?>(row, "PackageId") == command.PackageId, ct),
                        _ => throw new InvalidOperationException("Unknown metadata command.")
                    };
                    var entry = db.Entry(target);
                    if (Convert.ToBase64String((byte[])entry.Property("RowVersion").CurrentValue!) != command.ExpectedVersion)
                        throw new MetadataDenied(409, "concurrency_conflict");
                    var before = JsonSerializer.Serialize(command.Action switch
                    {
                        "locality" => (object)new { ((Project)target).Locality },
                        "organization" => new { ((RoadGuardSystem.BusinessObjects.Identity.ApplicationUser)target).Organization },
                        "package" => new { ((RepairPackage)target).Name, ((RepairPackage)target).Description, ((RepairPackage)target).PlannedDueAt },
                        _ => new { ((RepairItem)target).PlannedQuantity, ((RepairItem)target).PlannedUnit, ((RepairItem)target).PlannedMaterials }
                    }, Json);
                    if (target is Project project) project.Locality = command.Text;
                    else if (target is RoadGuardSystem.BusinessObjects.Identity.ApplicationUser user) user.Organization = command.Text;
                    else if (target is RepairPackage package)
                    {
                        package.Name = command.Text; package.Description = command.Description; package.PlannedDueAt = command.DueAt;
                        entry.Property("MutationRevision").CurrentValue = (long)entry.Property("MutationRevision").CurrentValue! + 1;
                    }
                    else if (target is RepairItem item) { item.PlannedQuantity = command.Quantity; item.PlannedUnit = command.Unit; item.PlannedMaterials = command.Materials; }
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, clock.GetUtcNow(), "web_metadata_" + command.Action,
                        entry.Metadata.ClrType.Name, command.TargetId, before, JsonSerializer.Serialize(new
                        { command.Text, command.Description, command.DueAt, command.Quantity, command.Unit, command.Materials }, Json),
                        command.Reason, "web.metadata.v1", Guid.NewGuid(), ["locality", "organization", "name", "description", "plannedDueAt", "plannedQuantity", "plannedUnit", "plannedMaterials", "text", "dueAt", "quantity", "unit", "materials"]));
                    await db.SaveChangesAsync(ct);
                    return (command.TargetId, JsonSerializer.Serialize(new WebMetadataResult(200, Version: Convert.ToBase64String((byte[])entry.Property("RowVersion").CurrentValue!)), Json));
                }, token, GuardWrite);
            return result.Status == IdempotencyOperationStatus.Conflict ? new(409, "duplicate_request") :
                JsonSerializer.Deserialize<WebMetadataResult>(result.OutcomeJson, Json)!;
        }
        catch (MetadataReceiptExists) { db.ChangeTracker.Clear(); return await WriteAsync(actor, role, command, token); }
        catch (MetadataDenied denied) { db.ChangeTracker.Clear(); return new(denied.Status, denied.Code); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }

        async Task GuardWrite(CancellationToken ct)
        {
            await Anh02ReceiptAuthority.LockAsync(db, actor, command.ProjectId, ct);
            var required = command.Action is "locality" or "organization" ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager;
            if (role != required || !await AuthorizeAsync(actor, role, command.ProjectId is Guid project ? [project] : [], ct))
                throw new MetadataDenied(403, "access_forbidden");
            var exists = command.Action switch
            {
                "locality" => await db.Projects.AnyAsync(row => row.Id == command.TargetId, ct),
                "organization" => await db.Users.AnyAsync(row => row.Id == command.TargetId, ct),
                "package" => await db.RepairPackages.AnyAsync(row => row.Id == command.TargetId && row.ProjectId == command.ProjectId, ct),
                "item" => await db.RepairItems.AnyAsync(row => row.Id == command.TargetId && row.ProjectId == command.ProjectId && EF.Property<Guid?>(row, "PackageId") == command.PackageId, ct),
                _ => false
            };
            if (!exists) throw new MetadataDenied(404, "not_found");
            if (command.Action is "package" or "item")
            {
                var obligationIds = command.Action == "package"
                    ? await db.RepairObligations.Where(row => EF.Property<Guid?>(row, "PackageId") == command.TargetId).Select(row => row.Id).ToArrayAsync(ct)
                    : await db.RepairItems.Where(row => row.Id == command.TargetId).Select(row => row.ObligationId).ToArrayAsync(ct);
                foreach (var id in obligationIds)
                    if (await ObligationResponsibilityScope.ResolveAsync(db, id, command.ProjectId!.Value, ct) != command.ProjectId)
                        throw new MetadataDenied(404, "not_found");
            }
        }
    }
    private sealed class MetadataReceiptExists : Exception;
    private sealed class MetadataDenied(int status, string code) : Exception
    { public int Status { get; } = status; public string Code { get; } = code; }
}
