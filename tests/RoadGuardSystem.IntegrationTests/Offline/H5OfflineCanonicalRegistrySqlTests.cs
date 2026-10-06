using System.Data;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Implementations.Offline;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Offline;

public sealed class H5OfflineCanonicalRegistrySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SignedApprovedBeforeReuseRetainsReporterOwnerAndRechecksActualSource(bool endSource, bool supplementSource)
    {
        var source = await SeedRawGuardSource(true, true, supplementSource); await using var db = sql.CreateDbContext();
        if (endSource)
        {
            (await db.Set<HuyDefectSourceLink>().SingleAsync(row => row.ProjectId == source.Scope.Project && row.DefectId == source.Scope.Defect && row.EndedAt == null))
                .EndedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        }
        var binding = await db.Set<OfflineOperationBinding>().AsNoTracking().SingleAsync(row => row.Id == source.BindingId);
        var declaration = Assert.Single(JsonSerializer.Deserialize<OfflineOperationInput>(binding.EnvelopeJson, Json)!.FieldSubmission!.Evidence!);
        var file = await db.Files.AsNoTracking().SingleAsync(row => row.Id == source.FileId);
        var privateScope = await db.FileScopes.AsNoTracking().SingleAsync(row => row.FileId == file.Id);
        Assert.Null(privateScope.ProjectId); Assert.NotEqual(source.Scope.Crew, privateScope.OwnerUserId);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        if (endSource)
        {
            var rejected = await Assert.ThrowsAsync<OfflineAdmissionRejectedException>(() => validator.ResolveDeclaredEvidenceAsync(
                source.Scope.Project, source.TaskId, source.AdmissionId, source.Scope.Crew,
                OfflineContractMapping.ToData(declaration), CancellationToken.None));
            Assert.Equal(403, rejected.Status); Assert.Equal("evidence_access_forbidden", rejected.Code);
        }
        else
        {
            var proof = await validator.ResolveDeclaredEvidenceAsync(source.Scope.Project, source.TaskId,
                source.AdmissionId, source.Scope.Crew, OfflineContractMapping.ToData(declaration), CancellationToken.None);
            Assert.NotNull(proof); Assert.Equal(file.Id, proof.FileId); Assert.Equal(file.UploadedByUserId, proof.ActualUploaderId);
            await db.SaveChangesAsync();
            var retained = await db.Set<OfflineAdmittedFileReference>().AsNoTracking().SingleAsync(row => row.AdmissionId == source.AdmissionId);
            Assert.Equal(privateScope.OwnerUserId, retained.ActualFileOwnerId);
            Assert.Equal(file.UploadedByUserId, retained.ActualUploadedById);
            Assert.Equal(source.Scope.Crew, retained.OriginalActorId);
        }
        Assert.Equal(binding.EnvelopeJson, await db.Set<OfflineOperationBinding>().Where(row => row.Id == binding.Id).Select(row => row.EnvelopeJson).SingleAsync());
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualSignedSyncCommitsOneTypedStartAndGuardsItsDurableReplay(bool endMembershipBeforeReplay)
    {
        var source = await SeedRawGuardSource(false); await using var db = sql.CreateDbContext();
        var retained = await db.Set<OfflineSyncBatch>().AsNoTracking().SingleAsync(row => row.Id == source.BatchId);
        var input = JsonSerializer.Deserialize<OfflineSignedBatchInput>(retained.AttachedPayloadJson!, Json)!;
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        var core = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System,
            validator, validator);
        var repository = new OfflineWorkflowRepository(db, TimeProvider.System, core, validator);
        var command = new OfflineWorkflowCommand(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project,
            "sync", OfflineContractMapping.ToData("sync", input), null, "sync-" + Guid.NewGuid().ToString("N"), null);
        var algorithms = OfflineContractMapping.RepositoryAlgorithms;
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        async Task<bool> Current(CancellationToken token) =>
            await guard.AuthorizeAsync(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project, token) is not null;
        var result = await repository.ExecuteAsync(command, algorithms, Current, CancellationToken.None);
        Assert.Equal(200, result.Status);
        var item = JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("items")[0];
        Assert.Equal(source.OriginId, item.GetProperty("effectId").GetGuid());
        Assert.True(item.GetProperty("durableAcknowledgment").GetBoolean());
        Assert.Equal("UNCERTAIN", item.GetProperty("timeProvenance").GetString());
        var start = await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(row => row.TaskId == source.TaskId);
        Assert.Equal(source.OriginId, start.Id); Assert.Equal(source.OriginId, start.OriginId);
        Assert.Null(start.VerifiedOriginalAt);
        Assert.Single(await db.Set<OfflineOperationResult>().Where(row => row.AdmissionId == source.AdmissionId && row.DurableAck).ToArrayAsync());
        var batchStatus = await repository.ExecuteAsync(command with
        {
            Action = "batch-get",
            Input = null,
            ResourceId = source.BatchId
        }, algorithms, Current, CancellationToken.None);
        Assert.Equal(200, batchStatus.Status);
        Assert.True(JsonSerializer.SerializeToElement(batchStatus.Value, Json).GetProperty("items")[0]
            .GetProperty("durableAcknowledgment").GetBoolean());
        var originStatus = await repository.ExecuteAsync(command with
        {
            Action = "origin-get",
            Input = null,
            ResourceId = source.OriginId
        }, algorithms, Current, CancellationToken.None);
        Assert.Equal(200, originStatus.Status);
        Assert.True(JsonSerializer.SerializeToElement(originStatus.Value, Json)
            .GetProperty("durableAcknowledgment").GetBoolean());
        var reconciled = await repository.ExecuteAsync(command with
        {
            Action = "start-reconcile",
            Input = OfflineContractMapping.ToData("start-reconcile",
                new OfflineStartReconcileInput(source.BatchId, source.OriginId)),
            Key = "reconcile-" + Guid.NewGuid().ToString("N")
        }, algorithms, Current, CancellationToken.None);
        Assert.Equal(200, reconciled.Status);
        Assert.True(JsonSerializer.SerializeToElement(reconciled.Value, Json).GetProperty("items")[0]
            .GetProperty("durableAcknowledgment").GetBoolean());
        if (endMembershipBeforeReplay)
        {
            (await db.ProjectMembers.SingleAsync(row => row.ProjectId == source.Scope.Project && row.UserId == source.Scope.Crew))
                .Status = ProjectMemberStatus.Ended;
            await db.SaveChangesAsync();
        }
        db.ChangeTracker.Clear();
        var replay = await repository.ExecuteAsync(command, algorithms, Current, CancellationToken.None);
        Assert.Equal(endMembershipBeforeReplay ? 403 : 200, replay.Status);
        Assert.Equal(1, await db.Set<FieldTaskStartOrigin>().CountAsync(row => row.TaskId == source.TaskId));
        Assert.Equal(1, await db.Set<FieldInspectionOperationOrigin>().CountAsync(row => row.ProjectId == source.Scope.Project && row.OriginId == source.OriginId));
    }

    [Fact]
    public async Task CurrentDeviceOwnerCanRevokeOnceAndOldSignedSourcesLoseAdmission()
    {
        var source = await SeedRawGuardSource(false); await using var db = sql.CreateDbContext();
        var binding = await db.Set<OfflineOperationBinding>().AsNoTracking().SingleAsync(row => row.Id == source.BindingId);
        var batch = await db.Set<OfflineSyncBatch>().AsNoTracking().SingleAsync(row => row.Id == source.BatchId);
        var input = JsonSerializer.Deserialize<OfflineSignedBatchInput>(batch.AttachedPayloadJson!, Json)!;
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        var core = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System,
            validator, validator);
        var repository = new OfflineWorkflowRepository(db, TimeProvider.System, core, validator);
        var algorithms = OfflineContractMapping.RepositoryAlgorithms;
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        async Task<bool> CurrentCrew(CancellationToken token) =>
            await guard.AuthorizeAsync(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project, token) is not null;
        async Task<bool> CurrentPm(CancellationToken token) =>
            await guard.AuthorizeAsync(source.Scope.Pm, UserRoleCode.ProjectManager, source.Scope.Project, token) is not null;
        var command = new OfflineWorkflowCommand(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project,
            "device-revoke", OfflineContractMapping.ToData("device-revoke",
                new OfflineDeviceRevokeInput("lost physical device")), binding.SourceDeviceRegistrationId,
            "revoke-" + Guid.NewGuid().ToString("N"), null);
        Assert.Equal(403, (await repository.ExecuteAsync(command with { ActorId = source.Scope.Pm, Role = UserRoleCode.ProjectManager },
            algorithms, CurrentPm, CancellationToken.None)).Status);
        Assert.Equal(201, (await repository.ExecuteAsync(command, algorithms, CurrentCrew, CancellationToken.None)).Status);
        Assert.Equal(200, (await repository.ExecuteAsync(command, algorithms, CurrentCrew, CancellationToken.None)).Status);
        var otherDeviceId = Guid.NewGuid(); using var otherKeys = OfflineDeviceKeys.Generate(source.Scope.Crew, otherDeviceId.ToString("D"));
        var otherDevice = OfflineDeviceRegistration.Register(Guid.NewGuid(), source.Scope.Project, source.Scope.Crew,
            otherDeviceId, 1, UserRoleCode.RepairCrew, otherKeys.PublicKeys.EncryptionPublicKey,
            otherKeys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow);
        db.Add(otherDevice); await db.SaveChangesAsync();
        Assert.Equal(409, (await repository.ExecuteAsync(command with { ResourceId = otherDevice.Id },
            algorithms, CurrentCrew, CancellationToken.None)).Status);
        Assert.False(await db.Set<OfflineDeviceRevocation>().AnyAsync(row => row.DeviceRegistrationId == otherDevice.Id));
        Assert.Equal(409, (await repository.ExecuteAsync(command with { Key = "another-" + Guid.NewGuid().ToString("N") },
            algorithms, CurrentCrew, CancellationToken.None)).Status);
        Assert.Single(await db.Set<OfflineDeviceRevocation>().Where(row => row.DeviceRegistrationId == binding.SourceDeviceRegistrationId).ToArrayAsync());
        Assert.Equal(1, await db.AuditLogs.CountAsync(row => row.EventType == "offline_device_revoked" && row.ActorUserId == source.Scope.Crew));
        var snapshot = new OfflineWorkflowCommand(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project,
            "snapshot-create", OfflineContractMapping.ToData("snapshot-create",
                new OfflineSnapshotInput(binding.SourceDeviceRegistrationId, source.TaskId)), null,
            "snapshot-after-revoke-" + Guid.NewGuid().ToString("N"), null);
        Assert.Equal(403, (await repository.ExecuteAsync(snapshot, algorithms, CurrentCrew, CancellationToken.None)).Status);
        var snapshotRead = new OfflineWorkflowCommand(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project,
            "snapshot-get", null, binding.SnapshotId, null, null);
        Assert.Equal(403, (await repository.ExecuteAsync(snapshotRead, algorithms, CurrentCrew, CancellationToken.None)).Status);
        var sync = new OfflineWorkflowCommand(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project,
            "sync", OfflineContractMapping.ToData("sync", input), null,
            "sync-after-revoke-" + Guid.NewGuid().ToString("N"), null);
        Assert.Equal(403, (await repository.ExecuteAsync(sync, algorithms, CurrentCrew, CancellationToken.None)).Status);
        (await db.ProjectMembers.SingleAsync(row => row.ProjectId == source.Scope.Project && row.UserId == source.Scope.Crew))
            .Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await repository.ExecuteAsync(command, algorithms, CurrentCrew, CancellationToken.None)).Status);
        Assert.Equal(1, await db.Set<OfflineDeviceRevocation>().CountAsync(row => row.DeviceRegistrationId == binding.SourceDeviceRegistrationId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualSnapshotPinsCurrentAssignmentAndDeniesAnEndedAssignment(bool endAssignment)
    {
        var source = await SeedRawGuardSource(false); await using var db = sql.CreateDbContext();
        var binding = await db.Set<OfflineOperationBinding>().AsNoTracking().SingleAsync(row => row.Id == source.BindingId);
        if (endAssignment)
        {
            (await db.FieldInspectionAssignments.SingleAsync(row => row.Id == binding.AssignmentId))
                .End(DateTimeOffset.UtcNow, "assignment ended before download");
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        }
        var repository = new OfflineWorkflowRepository(db, TimeProvider.System,
            new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db),
                TimeProvider.System));
        var algorithms = OfflineContractMapping.RepositoryAlgorithms;
        var command = new OfflineWorkflowCommand(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project,
            "snapshot-create", OfflineContractMapping.ToData("snapshot-create",
                new OfflineSnapshotInput(binding.SourceDeviceRegistrationId, source.TaskId)), null,
            "snapshot-" + Guid.NewGuid().ToString("N"), null);
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        async Task<bool> Current(CancellationToken token) =>
            await guard.AuthorizeAsync(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project, token) is not null;
        var count = await db.Set<OfflineTaskSnapshot>().CountAsync(row => row.ProjectId == source.Scope.Project);
        var result = await repository.ExecuteAsync(command, algorithms, Current, CancellationToken.None);
        Assert.Equal(endAssignment ? 403 : 201, result.Status);
        Assert.Equal(count + (endAssignment ? 0 : 1), await db.Set<OfflineTaskSnapshot>().CountAsync(row => row.ProjectId == source.Scope.Project));
        if (endAssignment) return;
        var id = JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("id").GetGuid();
        var snapshot = await db.Set<OfflineTaskSnapshot>().AsNoTracking().SingleAsync(row => row.Id == id);
        var task = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == source.TaskId);
        Assert.Equal(binding.AssignmentId, snapshot.AssignmentId);
        Assert.Equal(source.Scope.Crew, snapshot.OriginalActorId);
        Assert.Equal(Convert.ToBase64String(task.RowVersion), snapshot.TaskVersion);
        using var payload = JsonDocument.Parse(snapshot.SnapshotJson);
        Assert.Equal(source.Scope.Route, payload.RootElement.GetProperty("roadSectionVersionId").GetGuid());
        Assert.Equal(source.Scope.Set, payload.RootElement.GetProperty("segmentSetId").GetGuid());
        Assert.Equal(task.MeasurementScope, payload.RootElement.GetProperty("measurementScope").GetString());
        var location = payload.RootElement.GetProperty("location");
        Assert.Equal(source.Scope.Route, location.GetProperty("routeVersionId").GetGuid());
        Assert.Equal(source.Scope.Set, location.GetProperty("segmentSetId").GetGuid());
        Assert.Equal(32648, location.GetProperty("route").GetProperty("spatialSrid").GetInt32());
        Assert.True(location.GetProperty("segments").GetArrayLength() > 0);
        Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("repair").ValueKind);
        Assert.DoesNotContain("storageUri", snapshot.SnapshotJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reporterUserId", snapshot.SnapshotJson, StringComparison.OrdinalIgnoreCase);
        var read = new OfflineWorkflowCommand(source.Scope.Crew, UserRoleCode.RepairCrew, source.Scope.Project,
            "snapshot-get", null, snapshot.Id, null, null);
        var currentRead = await repository.ExecuteAsync(read, algorithms, Current, CancellationToken.None);
        Assert.Equal(200, currentRead.Status);
        Assert.Equal(snapshot.ContentHash, JsonSerializer.SerializeToElement(currentRead.Value, Json).GetProperty("contentHash").GetString());
        async Task<bool> CurrentPm(CancellationToken token) =>
            await guard.AuthorizeAsync(source.Scope.Pm, UserRoleCode.ProjectManager, source.Scope.Project, token) is not null;
        Assert.Equal(403, (await repository.ExecuteAsync(read with { ActorId = source.Scope.Pm, Role = UserRoleCode.ProjectManager },
            algorithms, CurrentPm, CancellationToken.None)).Status);
        (await db.FieldInspectionAssignments.SingleAsync(row => row.Id == binding.AssignmentId))
            .End(DateTimeOffset.UtcNow, "controlled stale snapshot read");
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await repository.ExecuteAsync(read, algorithms, Current, CancellationToken.None)).Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignedDeclarationResolvesActualVerifiedFileWithoutChangingOriginalPayload(bool changeChecksum)
    {
        var source = await SeedRawGuardSource(true); await using var db = sql.CreateDbContext();
        var binding = await db.Set<OfflineOperationBinding>().AsNoTracking().SingleAsync(row => row.Id == source.BindingId);
        var operation = JsonSerializer.Deserialize<OfflineOperationInput>(binding.EnvelopeJson, Json)!;
        var declaration = Assert.Single(operation.FieldSubmission!.Evidence!);
        if (changeChecksum) declaration = declaration with { ChecksumSha256 = new string('d', 64) };
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        if (changeChecksum)
        {
            var error = await Assert.ThrowsAsync<OfflineAdmissionRejectedException>(() => validator.ResolveDeclaredEvidenceAsync(
                source.Scope.Project, source.TaskId, source.AdmissionId, source.Scope.Crew,
                OfflineContractMapping.ToData(declaration), CancellationToken.None));
            Assert.Equal(409, error.Status); Assert.Equal("origin_content_conflict", error.Code);
            Assert.False(await db.Set<OfflineAdmittedFileReference>().AnyAsync(row => row.AdmissionId == source.AdmissionId));
        }
        else
        {
            var proof = await validator.ResolveDeclaredEvidenceAsync(source.Scope.Project, source.TaskId,
                source.AdmissionId, source.Scope.Crew, OfflineContractMapping.ToData(declaration), CancellationToken.None);
            Assert.NotNull(proof); Assert.Equal(source.FileId, proof.FileId);
            Assert.Equal(source.Scope.Crew, proof.OriginalActorId); Assert.Equal(source.Scope.Crew, proof.ActualUploaderId);
            Assert.Equal(declaration.ChecksumSha256, proof.Checksum); Assert.Equal("MEASUREMENT", proof.Purpose);
            await db.SaveChangesAsync();
            var retained = await db.Set<OfflineAdmittedFileReference>().AsNoTracking().SingleAsync(row => row.AdmissionId == source.AdmissionId);
            Assert.Equal(source.Scope.Crew, retained.ActualFileOwnerId); Assert.Equal(source.Scope.Crew, retained.ActualUploadedById);
        }
        Assert.Equal(binding.EnvelopeJson, await db.Set<OfflineOperationBinding>().Where(row => row.Id == binding.Id)
            .Select(row => row.EnvelopeJson).SingleAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task RawAcknowledgmentCannotClaimAnAdmittedBindingWithoutItsCanonicalTypedEffect()
    {
        var source = await SeedRawGuardSource(false); await using var db = sql.CreateDbContext();
        db.Add(OfflineOperationResult.Record(Guid.NewGuid(), source.Scope.Project, source.BatchId, source.AdmissionId,
            source.OriginId, source.OriginId, "COMMITTED", null, "UNCERTAIN", "UNKNOWN", null, null, "{}", null, DateTimeOffset.UtcNow));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(51401, Assert.IsType<SqlException>(error.InnerException).Number);
        db.ChangeTracker.Clear();
        Assert.False(await db.Set<OfflineOperationResult>().AnyAsync(row => row.AdmissionId == source.AdmissionId));
        Assert.False(await db.Set<FieldTaskStartOrigin>().AnyAsync(row => row.OriginId == source.OriginId));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("uploader")]
    public async Task RawAdmittedFileCannotFalsifyActualOwnerOrUploaderForGenuineSignedDeclaration(string changedFact)
    {
        var source = await SeedRawGuardSource(true); await using var db = sql.CreateDbContext();
        var file = await db.Files.AsNoTracking().SingleAsync(row => row.Id == source.FileId);
        var owner = changedFact == "owner" ? source.Scope.Pm : source.Scope.Crew;
        var uploader = changedFact == "uploader" ? source.Scope.Pm : source.Scope.Crew;
        db.Add(OfflineAdmittedFileReference.Capture(Guid.NewGuid(), source.Scope.Project, source.TaskId, source.AdmissionId,
            source.BindingId, source.CaptureOriginId!.Value, file.Id, source.Scope.Crew, source.Scope.Crew, owner, uploader,
            "MEASUREMENT", file.Checksum, file.Checksum, "{}", DateTimeOffset.UtcNow));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(51401, Assert.IsType<SqlException>(error.InnerException).Number);
        db.ChangeTracker.Clear();
        Assert.False(await db.Set<OfflineAdmittedFileReference>().AnyAsync(row => row.AdmissionId == source.AdmissionId));
        Assert.Equal(source.Scope.Crew, (await db.Files.AsNoTracking().SingleAsync(row => row.Id == file.Id)).UploadedByUserId);
    }

    private async Task<RawGuardSource> SeedRawGuardSource(bool withEvidence, bool reuseBefore = false,
        bool supplementBefore = false, bool unboundCapture = false)
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == scope.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await Command(db, scope, scope.Pm, UserRoleCode.ProjectManager, null, "create",
            new FieldTaskCreateInput(scope.Defect, defectVersion, null, "REPORTER", scope.Route, scope.Set,
                null, null, "PRE_MEASUREMENT", 1, "{}", "guard source", scope.Crew, DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Equal(201, created.Status);
        var taskId = JsonSerializer.SerializeToElement(created.Value, Json).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, taskId, "accept", new FieldTaskActionInput("accept"))).Status);
        Guid? firstStart = null;
        if (withEvidence)
        {
            var started = await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, taskId, "start",
                new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow));
            Assert.Equal(201, started.Status); firstStart = JsonSerializer.SerializeToElement(started.Value, Json).GetProperty("id").GetGuid();
        }
        db.ChangeTracker.Clear(); var now = DateTimeOffset.UtcNow;
        var task = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == taskId);
        var assignment = await db.FieldInspectionAssignments.AsNoTracking().SingleAsync(row => row.FieldInspectionTaskId == taskId && row.Status == FieldInspectionAssignmentStatus.Active);
        var deviceId = Guid.NewGuid(); using var keys = OfflineDeviceKeys.Generate(scope.Crew, deviceId.ToString("D"));
        var registration = OfflineDeviceRegistration.Register(Guid.NewGuid(), scope.Project, scope.Crew, deviceId, 1,
            UserRoleCode.RepairCrew, keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey, now);
        var snapshot = OfflineTaskSnapshot.Capture(Guid.NewGuid(), scope.Project, taskId, assignment.Id, scope.Crew,
            registration.Id, Convert.ToBase64String(task.RowVersion), new string('a', 64),
            JsonSerializer.Serialize(new { taskId, assignmentId = assignment.Id, routeVersionId = scope.Route, segmentSetId = scope.Set }, Json), now);
        Guid? fileId = null; Guid? captureOrigin = null; var origin = Guid.NewGuid();
        OfflineOperationInput operation;
        if (withEvidence)
        {
            StoredFile file;
            if (reuseBefore)
            {
                var reportId = await db.Set<HuyDefectSourceLink>().Where(row => row.ProjectId == scope.Project && row.DefectId == scope.Defect && row.EndedAt == null)
                    .Select(row => row.ReportSourceId!.Value).SingleAsync();
                var evidence = await db.Reports.AsNoTracking().Where(row => row.Id == reportId).SelectMany(row => row.OriginalEvidence).SingleAsync();
                file = await db.Files.AsNoTracking().SingleAsync(row => row.Id == evidence.FileId);
                var evidenceId = evidence.Id;
                if (supplementBefore)
                {
                    evidenceId = Guid.NewGuid();
                    var report = await db.Reports.SingleAsync(row => row.Id == reportId);
                    report.AddSupplement(Guid.NewGuid(), "controlled supplement for approved BEFORE reuse",
                        [VerifiedEvidenceReference.Create(evidenceId, file.Id, evidence.FileVersion, evidence.OwnerUserId)], now);
                    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
                }
                Assert.Equal(201, (await Command(db, scope, scope.Pm, UserRoleCode.ProjectManager, taskId, "reuse",
                    new FieldEvidenceReuseInput(file.Id, evidenceId, "REPORTER", file.Checksum, "actual approved BEFORE source"))).Status);
            }
            else
            {
                file = StoredFile.Create(Guid.NewGuid(), "field/guard-" + Guid.NewGuid().ToString("N"), "measurement.jpg",
                    "image/jpeg", 4, new string('c', 64), scope.Crew, now, null);
                db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, scope.Project, taskId, scope.Crew, "MEASUREMENT", now)); await db.SaveChangesAsync();
                var upload = UploadSession.Create(Guid.NewGuid(), file.Id, scope.Crew, file.StorageUri, "MEASUREMENT", file.MimeType,
                    file.SizeBytes, file.Checksum, 8388608, now.AddHours(24)); upload.StartUploading("fixture", now);
                db.Add(upload); await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
                await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
            }
            fileId = file.Id; captureOrigin = Guid.NewGuid();
            var body = new FieldSubmissionInput(origin, firstStart!.Value, null,
                [new("zero", "Length", 0, "KNOWN", null, "LENGTH", "mm", null, null, "GPS unavailable", "gauge", "measurement")],
                [new(captureOrigin.Value, unboundCapture ? null : file.Id, reuseBefore ? "BEFORE" : "MEASUREMENT",
                    file.Checksum, file.MimeType, now, null)],
                null, "MEASUREMENT", null, null, deviceId);
            operation = new(1, origin, origin, "FIELD_SUBMISSION", scope.Crew, deviceId, snapshot.Id, taskId, assignment.Id,
                snapshot.TaskVersion, new string('0', 64), [], null, FieldSubmission: body);
        }
        else
            operation = new(1, origin, origin, "FIELD_START", scope.Crew, deviceId, snapshot.Id, taskId, assignment.Id,
                snapshot.TaskVersion, new string('0', 64), [], null, new FieldStartInput(origin, now, deviceId));
        operation = operation with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(operation) };
        var descriptor = OfflineWorkflowEngine.Describe(operation);
        var binding = OfflineOperationBinding.Bind(Guid.NewGuid(), scope.Project, origin, origin, operation.Kind,
            descriptor.CorePayloadHash, descriptor.EnvelopeHash, scope.Crew, registration.Id, taskId, assignment.Id, snapshot.Id,
            JsonSerializer.Serialize(operation, Json), now);
        var sourceBatch = Guid.NewGuid(); var claim = OfflineWorkflowEngine.CanonicalManifest(scope.Project, sourceBatch, registration.Id, [descriptor]);
        var signature = OfflinePackageAuthentication.SignClaim(claim, keys);
        var batch = OfflineSyncBatch.ReceiveAuthenticated(Guid.NewGuid(), scope.Project, sourceBatch, registration.Id, scope.Crew,
            null, null, Encoding.UTF8.GetString(claim), signature,
            Encoding.UTF8.GetString(OfflineWorkflowEngine.CanonicalAttachedPayload(sourceBatch, registration.Id, [operation], signature)), now);
        var admission = OfflineOperationAdmission.Record(Guid.NewGuid(), scope.Project, batch.Id, binding.Id, scope.Crew,
            UserRoleCode.RepairCrew, null, "{}", now);
        db.AddRange(registration, snapshot, binding, batch, admission); await db.SaveChangesAsync();
        return new(scope, taskId, origin, binding.Id, batch.Id, admission.Id, fileId, captureOrigin);
    }

    private sealed record RawGuardSource(Scope Scope, Guid TaskId, Guid OriginId, Guid BindingId, Guid BatchId,
        Guid AdmissionId, Guid? FileId, Guid? CaptureOriginId);

    [Fact]
    public async Task SignedUnboundCaptureCreatesOneActualUploaderFileAndGuardsUploadReceipt()
    {
        var source = await SeedRawGuardSource(true, unboundCapture: true);
        await using var db = sql.CreateDbContext();
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        var storage = new ControlledUploadStorage();
        var repository = new UploadPersistenceService(db, new IdempotencyOperationService(db),
            storage, Options.Create(new UploadSessionOptions()), TimeProvider.System, validator);
        var key = "offline-capture-" + Guid.NewGuid().ToString("N");
        var upload = new UploadCreatePersistenceRequest(source.Scope.Crew, source.Scope.Project, source.TaskId,
            "MEASUREMENT", "signed-capture.jpg", "image/jpeg", 4, new string('c', 64), 8388608,
            DateTimeOffset.UtcNow.AddHours(24), key, new string('d', 64),
            source.OriginId);
        var request = new OfflineUploadCaptureRequest(source.Scope.Project, source.TaskId, source.AdmissionId,
            source.CaptureOriginId!.Value, source.Scope.Crew, UserRoleCode.RepairCrew, upload);
        var created = await repository.CreateOfflineAsync(request);
        Assert.Equal(UploadPersistenceStatus.Success, created.Status);
        Assert.NotNull(created.Session);
        var capture = await db.Set<OfflineEvidenceCaptureReference>().AsNoTracking().SingleAsync(row =>
            row.AdmissionId == source.AdmissionId && row.CaptureOriginId == source.CaptureOriginId);
        Assert.Equal(created.Session.FileId, capture.FileId);
        Assert.Equal(created.Session.Id, capture.UploadSessionId);
        Assert.Equal(source.Scope.Crew, capture.ActualUploaderId);
        Assert.Equal(source.Scope.Crew, capture.OriginalActorId);
        Assert.Equal(1, await db.Set<OfflineEvidenceCaptureReference>().CountAsync(row =>
            row.AdmissionId == source.AdmissionId));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(row => row.Operation ==
            "OfflineUploadSessionCreated" && row.IdempotencyKey == key));
        db.ChangeTracker.Clear();
        Assert.Equal(UploadPersistenceStatus.Replayed, (await repository.CreateOfflineAsync(request)).Status);
        Assert.Equal(UploadPersistenceStatus.NotFound, (await repository.CreateOfflineAsync(request with
        {
            Upload = upload with { IdempotencyKey = "second-" + Guid.NewGuid().ToString("N") }
        })).Status);
        Assert.Equal(1, await db.Set<OfflineEvidenceCaptureReference>().CountAsync(row =>
            row.AdmissionId == source.AdmissionId));
        Assert.True(await repository.IsCurrentOfflineFileActorAsync(source.Scope.Crew,
            UserRoleCode.RepairCrew, created.Session.FileId));
        var partKey = "part-" + Guid.NewGuid().ToString("N");
        var partNow = DateTimeOffset.UtcNow;
        var parts = await repository.GetPartUrlsAsync(source.Scope.Crew, source.Scope.Project,
            created.Session.Id, [1], partKey, new string('e', 64), partNow,
            partNow.AddMinutes(10));
        Assert.Equal(UploadPersistenceStatus.Success, parts.Status);
        Assert.Single(parts.Parts);
        Assert.Equal(UploadPersistenceStatus.Replayed, (await repository.GetPartUrlsAsync(source.Scope.Crew,
            source.Scope.Project, created.Session.Id, [1], partKey, new string('e', 64),
            partNow, partNow.AddMinutes(10))).Status);
        Assert.Equal(1, storage.Initiations);
        var current = await repository.GetSessionAsync(created.Session.Id);
        Assert.NotNull(current);
        var completed = new UploadCompletePersistenceRequest(source.Scope.Crew, source.Scope.Project,
            created.Session.Id, current.Version, [new CompletedStoragePart(1, "part-etag")],
            upload.ChecksumSha256, "complete-" + Guid.NewGuid().ToString("N"), new string('f', 64), null);
        Assert.Equal(UploadPersistenceStatus.Success, (await repository.CompleteAsync(completed)).Status);
        Assert.Equal(UploadPersistenceStatus.Replayed, (await repository.CompleteAsync(completed)).Status);
        Assert.Equal(1, await db.Set<OfflineEvidenceCaptureReference>().CountAsync(row =>
            row.AdmissionId == source.AdmissionId));
        (await db.ProjectMembers.SingleAsync(row => row.ProjectId == source.Scope.Project &&
            row.UserId == source.Scope.Crew)).Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(UploadPersistenceStatus.NotFound, (await repository.CreateOfflineAsync(request)).Status);
        Assert.False(await repository.IsCurrentOfflineFileActorAsync(source.Scope.Crew,
            UserRoleCode.RepairCrew, created.Session.FileId));
        Assert.Equal(UploadPersistenceStatus.NotFound, (await repository.GetPartUrlsAsync(source.Scope.Crew,
            source.Scope.Project, created.Session.Id, [1], partKey, new string('e', 64),
            partNow, partNow.AddMinutes(10))).Status);
        Assert.Equal(UploadPersistenceStatus.NotFound, (await repository.CompleteAsync(completed)).Status);
        Assert.Equal(1, await db.Set<OfflineEvidenceCaptureReference>().CountAsync(row =>
            row.AdmissionId == source.AdmissionId));
    }

    private sealed class ControlledUploadStorage : IUploadObjectStorage
    {
        public int Initiations { get; private set; }
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
        {
            Initiations++;
            return Task.FromResult("controlled-multipart");
        }
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId,
            IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(number =>
                new PresignedUploadPart(number, "https://storage.invalid/part", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId,
            IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Create must not verify storage.");
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Create must not open storage.");
    }

    [Fact]
    public async Task SignedPackageGrantImportCommitsOneOriginalCrewEffectAndReplaysUnderCurrentRecipient()
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == scope.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await Command(db, scope, scope.Pm, UserRoleCode.ProjectManager, null, "create",
            new FieldTaskCreateInput(scope.Defect, defectVersion, null, "REPORTER", scope.Route, scope.Set,
                null, null, "PRE_MEASUREMENT", 1, "{}", "signed handover", scope.Crew, DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Equal(201, created.Status);
        var taskId = JsonSerializer.SerializeToElement(created.Value, Json).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, taskId, "accept",
            new FieldTaskActionInput("take task"))).Status);
        db.ChangeTracker.Clear();
        var task = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == taskId);
        var assignment = await db.FieldInspectionAssignments.AsNoTracking().SingleAsync(row =>
            row.FieldInspectionTaskId == taskId && row.Status == FieldInspectionAssignmentStatus.Active);
        var now = DateTimeOffset.UtcNow; var sourceDevice = Guid.NewGuid(); var recipientDevice = Guid.NewGuid();
        using var sourceKeys = OfflineDeviceKeys.Generate(scope.Crew, sourceDevice.ToString("D"));
        using var recipientKeys = OfflineDeviceKeys.Generate(scope.Pm, recipientDevice.ToString("D"));
        var source = OfflineDeviceRegistration.Register(Guid.NewGuid(), scope.Project, scope.Crew, sourceDevice, 1,
            UserRoleCode.RepairCrew, sourceKeys.PublicKeys.EncryptionPublicKey, sourceKeys.PublicKeys.SigningPublicKey, now);
        var recipient = OfflineDeviceRegistration.Register(Guid.NewGuid(), scope.Project, scope.Pm, recipientDevice, 1,
            UserRoleCode.ProjectManager, recipientKeys.PublicKeys.EncryptionPublicKey, recipientKeys.PublicKeys.SigningPublicKey, now);
        var snapshot = OfflineTaskSnapshot.Capture(Guid.NewGuid(), scope.Project, taskId, assignment.Id, scope.Crew,
            source.Id, Convert.ToBase64String(task.RowVersion), new string('a', 64),
            JsonSerializer.Serialize(new { taskId, assignmentId = assignment.Id }, Json), now);
        var supervisor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "handover supervisor",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = now
        };
        db.AddRange(source, recipient, snapshot, supervisor, new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = scope.Project,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            ValidFrom = new(2000, 1, 1),
            Status = ProjectMemberStatus.Active
        });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var origin = Guid.NewGuid();
        var operation = new OfflineOperationInput(1, origin, origin, "FIELD_START", scope.Crew, sourceDevice,
            snapshot.Id, taskId, assignment.Id, snapshot.TaskVersion, new string('0', 64), [], null,
            new FieldStartInput(origin, now, sourceDevice));
        operation = operation with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(operation) };
        var descriptor = OfflineWorkflowEngine.Describe(operation);
        var sourceBatch = Guid.NewGuid();
        var signedManifest = OfflineWorkflowEngine.CanonicalManifest(scope.Project, sourceBatch, source.Id, [descriptor]);
        var sourceSignature = OfflinePackageAuthentication.SignClaim(signedManifest, sourceKeys);
        var attached = OfflineWorkflowEngine.CanonicalAttachedPayload(sourceBatch, source.Id, [operation], sourceSignature);
        var attachedHash = Convert.ToHexString(SHA256.HashData(attached)).ToLowerInvariant();
        var packageId = Guid.NewGuid();
        var header = OfflinePackageHeader.Create(packageId, scope.Project, scope.Crew,
            sourceDevice.ToString("D"), [origin], attachedHash);
        var encrypted = OfflineHandoverCrypto.Seal(header, attached, sourceKeys, [recipientKeys.PublicKeys]);
        var manifestInput = new OfflineSourceManifestInput(sourceBatch, source.Id, [descriptor], sourceSignature);
        var repository = new OfflineWorkflowRepository(db, TimeProvider.System,
            new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System,
                new OfflineAdmissionValidator(db, TimeProvider.System), new OfflineAdmissionValidator(db, TimeProvider.System)),
            new OfflineAdmissionValidator(db, TimeProvider.System));
        var algorithms = OfflineContractMapping.RepositoryAlgorithms;
        var scopeGuard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        Task<bool> Current(Guid actor, UserRoleCode role, CancellationToken token)
            => CurrentAsync(actor, role, token);
        async Task<bool> CurrentAsync(Guid actor, UserRoleCode role, CancellationToken token)
            => await scopeGuard.AuthorizeAsync(actor, role, scope.Project, token) is not null;
        Task<OfflineWorkflowFact> Run(Guid actor, UserRoleCode role, string action, object input, string key,
            Guid? resourceId = null)
            => repository.ExecuteAsync(new(actor, role, scope.Project, action,
                    OfflineContractMapping.ToData(action, input), resourceId, key, null), algorithms,
                token => Current(actor, role, token), CancellationToken.None);
        var exported = await Run(scope.Crew, UserRoleCode.RepairCrew, "package-export",
            new OfflinePackageExportInput(H5EncryptedPackageDto.FromDomain(encrypted), manifestInput), "export-" + Guid.NewGuid());
        Assert.Equal(201, exported.Status);
        var grantInput = new OfflineHandoverGrantInput(packageId, recipient.Id,
            [new(origin, descriptor.Kind, descriptor.CorePayloadHash, descriptor.EnvelopeHash,
                taskId, assignment.Id, snapshot.Id)], "recover signed work");
        var granted = await Run(supervisor.Id, UserRoleCode.Supervisor, "grant-issue", grantInput,
            "grant-" + Guid.NewGuid());
        Assert.Equal(201, granted.Status);
        var grantId = JsonSerializer.SerializeToElement(granted.Value, Json).GetProperty("id").GetGuid();
        var grantRow = await db.Set<OfflineHandoverGrant>().AsNoTracking().SingleAsync(row => row.Id == grantId);
        var handoverClock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(row =>
            row.ProjectId == scope.Project && row.Kind == DeadlineClockKind.DeviceHandover &&
            row.TargetId == grantId);
        Assert.Equal(grantId, handoverClock.OriginEventId);
        Assert.Equal(grantRow.IssuedAt, handoverClock.OriginAt);
        Assert.Equal(grantRow.ExpiresAt, handoverClock.OriginalDueAt);
        var importBatchId = Guid.NewGuid();
        var endorsement = OfflineRecipientEndorsement.CanonicalClaim(new(scope.Project, packageId,
            grantId, importBatchId, recipient.Id, attachedHash));
        var recipientSignature = OfflinePackageAuthentication.SignClaim(endorsement, recipientKeys);
        var import = new OfflinePackageImportInput(importBatchId, packageId, grantId, recipient.Id,
            [operation], sourceSignature, recipientSignature);
        async Task<OfflineWorkflowFact> ImportConcurrentAsync()
        {
            await using var isolated = sql.CreateDbContext();
            var isolatedClock = TimeProvider.System;
            var isolatedAdmission = new OfflineAdmissionValidator(isolated, isolatedClock);
            var isolatedCore = new FieldInspectionWorkflowRepository(isolated,
                new IdempotencyOperationService(isolated), isolatedClock,
                isolatedAdmission, isolatedAdmission);
            var isolatedRepository = new OfflineWorkflowRepository(isolated, isolatedClock,
                isolatedCore, isolatedAdmission);
            var isolatedGuard = new ProjectScopeGuard(new ProjectMembershipReadModel(isolated), isolatedClock);
            return await isolatedRepository.ExecuteAsync(new(scope.Pm, UserRoleCode.ProjectManager,
                    scope.Project, "import", OfflineContractMapping.ToData("import", import), null,
                    "parallel-import-" + Guid.NewGuid(), null), algorithms,
                async token => await isolatedGuard.AuthorizeAsync(scope.Pm, UserRoleCode.ProjectManager,
                    scope.Project, token) is not null, CancellationToken.None);
        }
        var concurrent = await Task.WhenAll(ImportConcurrentAsync(), ImportConcurrentAsync());
        Assert.All(concurrent, result =>
        {
            Assert.Equal(200, result.Status);
            Assert.True(JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("items")[0]
                .GetProperty("durableAcknowledgment").GetBoolean());
        });
        Assert.Equal(200, (await Run(scope.Pm, UserRoleCode.ProjectManager, "import", import,
            "import-replay-" + Guid.NewGuid())).Status);
        Assert.Equal(1, await db.Set<FieldTaskStartOrigin>().CountAsync(row => row.TaskId == taskId));
        Assert.Equal(1, await db.Set<FieldInspectionOperationOrigin>().CountAsync(row =>
            row.ProjectId == scope.Project && row.OriginId == origin));
        Assert.Equal(1, await db.Set<OfflineOperationResult>().CountAsync(row =>
            row.OriginId == origin && row.DurableAck));

        var tamperedOperation = operation with { FieldStart = new FieldStartInput(origin, now.AddMinutes(1), sourceDevice) };
        tamperedOperation = tamperedOperation with
        {
            CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(tamperedOperation)
        };
        var tampered = import with { Operations = [tamperedOperation] };
        var rejectedTamper = await Run(scope.Pm, UserRoleCode.ProjectManager, "import", tampered,
            "tampered-" + Guid.NewGuid());
        Assert.Equal(409, rejectedTamper.Status);
        Assert.Equal("origin_content_conflict", rejectedTamper.Code);
        Assert.Equal(1, await db.Set<OfflineOperationResult>().CountAsync(row => row.OriginId == origin && row.DurableAck));

        var afterExpiry = new FixedClock(grantRow.ExpiresAt.AddMinutes(1));
        var expiredRepository = new OfflineWorkflowRepository(db, afterExpiry,
            new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), afterExpiry,
                new OfflineAdmissionValidator(db, afterExpiry), new OfflineAdmissionValidator(db, afterExpiry)),
            new OfflineAdmissionValidator(db, afterExpiry));
        Task<OfflineWorkflowFact> RunExpired(OfflinePackageImportInput body, string key)
            => expiredRepository.ExecuteAsync(new(scope.Pm, UserRoleCode.ProjectManager, scope.Project, "import",
                    OfflineContractMapping.ToData("import", body), null, key, null), algorithms,
                token => Current(scope.Pm, UserRoleCode.ProjectManager, token), CancellationToken.None);
        var lateBatchId = Guid.NewGuid();
        var lateEndorsement = OfflineRecipientEndorsement.CanonicalClaim(new(scope.Project, packageId,
            grantId, lateBatchId, recipient.Id, attachedHash));
        var lateImport = import with
        {
            BatchId = lateBatchId,
            RecipientSignature = OfflinePackageAuthentication.SignClaim(lateEndorsement, recipientKeys)
        };
        var expiredNew = await RunExpired(lateImport, "expired-new-" + Guid.NewGuid());
        Assert.Equal(403, expiredNew.Status);
        Assert.Equal("handover_grant_expired", expiredNew.Code);
        var committedReplay = await RunExpired(import, "expired-replay-" + Guid.NewGuid());
        Assert.Equal(200, committedReplay.Status);
        Assert.Equal(1, await db.Set<OfflineOperationResult>().CountAsync(row => row.OriginId == origin && row.DurableAck));

        (await db.Users.SingleAsync(row => row.Id == scope.Crew)).Status = UserStatus.Suspended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(200, (await RunExpired(import, "recipient-current-" + Guid.NewGuid())).Status);
        var revoked = await Run(supervisor.Id, UserRoleCode.Supervisor, "grant-revoke",
            new OfflineGrantRevokeInput("recipient key compromised"), "revoke-" + Guid.NewGuid(), grantId);
        Assert.Equal(201, revoked.Status);
        var deniedReplay = await RunExpired(import, "revoked-replay-" + Guid.NewGuid());
        Assert.Equal(403, deniedReplay.Status);
        Assert.Equal("handover_grant_revoked", deniedReplay.Code);
        Assert.Equal(1, await db.Set<FieldTaskStartOrigin>().CountAsync(row => row.TaskId == taskId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualDeviceRegistrationHasOneDurableEffectAndRechecksCallerBeforeReceipt(bool endMembershipBeforeReplay)
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        var device = Guid.NewGuid(); using var keys = OfflineDeviceKeys.Generate(scope.Crew, device.ToString("D"));
        var body = new OfflineDeviceRegisterInput(device, keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey);
        var repository = new OfflineWorkflowRepository(db, TimeProvider.System);
        var command = new OfflineWorkflowCommand(scope.Crew, UserRoleCode.RepairCrew, scope.Project, "device-register",
            OfflineContractMapping.ToData("device-register", body), null,
            "registration-" + Guid.NewGuid().ToString("N"), null);
        var algorithms = OfflineContractMapping.RepositoryAlgorithms;
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        Task<bool> Current(CancellationToken token) => Authorized(token);
        async Task<bool> Authorized(CancellationToken token) =>
            await guard.AuthorizeAsync(scope.Crew, UserRoleCode.RepairCrew, scope.Project, token) is not null;
        var created = await repository.ExecuteAsync(command, algorithms, Current, CancellationToken.None);
        Assert.Equal(201, created.Status);
        Assert.Single(await db.Set<OfflineDeviceRegistration>().Where(row => row.ProjectId == scope.Project && row.DeviceId == device).ToArrayAsync());
        if (endMembershipBeforeReplay)
        {
            (await db.ProjectMembers.SingleAsync(row => row.ProjectId == scope.Project && row.UserId == scope.Crew)).Status = ProjectMemberStatus.Ended;
            await db.SaveChangesAsync();
        }
        db.ChangeTracker.Clear();
        var replay = await repository.ExecuteAsync(command, algorithms, Current, CancellationToken.None);
        Assert.Equal(endMembershipBeforeReplay ? 403 : 200, replay.Status);
        Assert.Equal(!endMembershipBeforeReplay, replay.Replayed);
        Assert.Equal(1, await db.Set<OfflineDeviceRegistration>().CountAsync(row => row.ProjectId == scope.Project && row.DeviceId == device));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(row => row.ActorUserId == scope.Crew && row.ProjectId == scope.Project &&
            row.Operation == "h5.offline.device-register.v1" && row.IdempotencyKey == command.Key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistedSignedAdmissionBindsActualOriginalAssignmentAndDoesNotPromoteClockClaims(bool wrongSignature)
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == scope.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await Command(db, scope, scope.Pm, UserRoleCode.ProjectManager, null, "create",
            new FieldTaskCreateInput(scope.Defect, defectVersion, null, "REPORTER", scope.Route, scope.Set,
                null, null, "PRE_MEASUREMENT", 1, "{}", "actual offline source", scope.Crew, DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Equal(201, created.Status);
        var taskId = JsonSerializer.SerializeToElement(created.Value, Json).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, taskId, "accept",
            new FieldTaskActionInput("actual Crew accepts"))).Status);
        db.ChangeTracker.Clear();
        var task = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == taskId);
        var assignment = await db.FieldInspectionAssignments.AsNoTracking().SingleAsync(row =>
            row.FieldInspectionTaskId == taskId && row.Status == FieldInspectionAssignmentStatus.Active);
        var now = DateTimeOffset.UtcNow; var deviceId = Guid.NewGuid();
        using var sourceKeys = OfflineDeviceKeys.Generate(scope.Crew, deviceId.ToString("D"));
        using var unrelatedKeys = OfflineDeviceKeys.Generate(scope.Crew, Guid.NewGuid().ToString("D"));
        var registration = OfflineDeviceRegistration.Register(Guid.NewGuid(), scope.Project, scope.Crew, deviceId, 1,
            UserRoleCode.RepairCrew, sourceKeys.PublicKeys.EncryptionPublicKey, sourceKeys.PublicKeys.SigningPublicKey, now);
        var assignmentJson = JsonSerializer.Serialize(new { assignment.Id, assignment.FieldInspectionTaskId, assignment.AssignedToUserId }, Json);
        var assignmentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(assignmentJson))).ToLowerInvariant();
        var snapshot = OfflineTaskSnapshot.Capture(Guid.NewGuid(), scope.Project, taskId, assignment.Id, scope.Crew,
            registration.Id, Convert.ToBase64String(task.RowVersion), assignmentHash,
            JsonSerializer.Serialize(new
            {
                taskId,
                assignmentId = assignment.Id,
                routeVersionId = scope.Route,
                segmentSetId = scope.Set,
                mode = "MEASURE_ONLY",
                purpose = "PRE_MEASUREMENT"
            }, Json), now);
        var origin = Guid.NewGuid();
        var body = new FieldStartInput(origin, now.AddDays(-3), DeviceId: deviceId);
        var operation = new OfflineOperationInput(1, origin, origin, "FIELD_START", scope.Crew, deviceId, snapshot.Id,
            taskId, assignment.Id, snapshot.TaskVersion, new string('0', 64), [], null, body);
        operation = operation with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(operation) };
        var descriptor = OfflineWorkflowEngine.Describe(operation);
        var binding = OfflineOperationBinding.Bind(Guid.NewGuid(), scope.Project, origin, origin, operation.Kind,
            descriptor.CorePayloadHash, descriptor.EnvelopeHash, scope.Crew, registration.Id, taskId, assignment.Id,
            snapshot.Id, JsonSerializer.Serialize(operation, Json), now);
        var sourceBatchId = Guid.NewGuid();
        var claim = OfflineWorkflowEngine.CanonicalManifest(scope.Project, sourceBatchId, registration.Id, [descriptor]);
        var signature = OfflinePackageAuthentication.SignClaim(claim, wrongSignature ? unrelatedKeys : sourceKeys);
        var batch = OfflineSyncBatch.ReceiveAuthenticated(Guid.NewGuid(), scope.Project, sourceBatchId, registration.Id, scope.Crew,
            null, null, Encoding.UTF8.GetString(claim), signature,
            Encoding.UTF8.GetString(OfflineWorkflowEngine.CanonicalAttachedPayload(sourceBatchId, registration.Id, [operation], signature)), now);
        var admission = OfflineOperationAdmission.Record(Guid.NewGuid(), scope.Project, batch.Id, binding.Id, scope.Crew,
            UserRoleCode.RepairCrew, null, JsonSerializer.Serialize(new { taskId, assignmentId = assignment.Id }, Json), now);
        db.AddRange(registration, snapshot, binding, batch, admission); await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var command = new FieldWorkflowCommand(scope.Project, taskId, "start", body, null, snapshot.TaskVersion,
            new(scope.Crew, UserRoleCode.RepairCrew, scope.Crew, "SYNC", false, null, admission.Id));
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        if (wrongSignature)
            await Assert.ThrowsAsync<CryptographicException>(() => validator.ValidateAsync(command, CancellationToken.None));
        else
        {
            var facts = await validator.ValidateAsync(command, CancellationToken.None);
            Assert.NotNull(facts); Assert.Equal(admission.Id, facts.AdmissionId); Assert.Equal(origin, facts.EffectId);
            Assert.Equal(scope.Crew, facts.OriginalActorId); Assert.Equal(assignment.Id, facts.AssignmentId);
            Assert.Equal(deviceId, facts.SourceDeviceId); Assert.Equal("UNCERTAIN", facts.TimeProvenance);
            Assert.Null(facts.VerifiedOriginalAt);
        }
        Assert.False(await db.Set<FieldTaskStartOrigin>().AnyAsync(row => row.TaskId == taskId));
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("membership-ended")]
    public async Task ImporterCurrentAuthorityIsCheckedBeforeReadingProtectedAdmission(string change)
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        if (change == "disabled")
            (await db.Users.SingleAsync(x => x.Id == scope.Crew)).Status = UserStatus.Suspended;
        else
            (await db.ProjectMembers.SingleAsync(x => x.ProjectId == scope.Project && x.UserId == scope.Crew))
                .Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var command = new FieldWorkflowCommand(scope.Project, Guid.NewGuid(), "start", null, null, null,
            new(scope.Crew, UserRoleCode.RepairCrew, Guid.NewGuid(), "HANDOVER", false, Guid.NewGuid(), Guid.NewGuid()));
        var error = await Assert.ThrowsAsync<OfflineAdmissionRejectedException>(() =>
            new OfflineAdmissionValidator(db, TimeProvider.System).ValidateAsync(command, CancellationToken.None));
        Assert.Equal(403, error.Status); Assert.Equal("access_forbidden", error.Code);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task GenuineExistingDirectStartKeepsItsOriginalNullDeviceAndActualEffectOnHistoricalDescription()
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        var version = Convert.ToBase64String(await db.Defects.Where(x => x.Id == scope.Defect)
            .Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var created = await Command(db, scope, scope.Pm, UserRoleCode.ProjectManager, null, "create",
            new FieldTaskCreateInput(scope.Defect, version, null, "REPORTER", scope.Route, scope.Set, null,
                null, "PRE_MEASUREMENT", 1, "{}", "measure only", scope.Crew, DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Equal(201, created.Status);
        var task = JsonSerializer.SerializeToElement(created.Value, Json).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, task, "accept",
            new FieldTaskActionInput("actual Crew accepts"))).Status);
        var body = new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(201, (await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, task, "start", body)).Status);
        var canonical = await db.Set<FieldInspectionOperationOrigin>().AsNoTracking()
            .SingleAsync(x => x.ProjectId == scope.Project && x.OriginId == body.OriginId);
        var start = await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(x => x.Id == canonical.EffectId);
        var currentVersion = Convert.ToBase64String(await db.FieldInspectionTasks.Where(x => x.Id == task)
            .Select(x => x.RowVersion).SingleAsync());
        var operation = new OfflineOperationInput(1, body.OriginId, canonical.EffectId, "FIELD_START",
            scope.Crew, Guid.NewGuid(), Guid.NewGuid(), task, start.AssignmentId, currentVersion,
            canonical.ContentHash, [], null, body);
        var descriptor = OfflineWorkflowEngine.DescribeHistoricalStart(operation, canonical, start);
        Assert.Equal(start.Id, descriptor.EffectId);
        Assert.Null(body.DeviceId); Assert.Null(start.DeviceId); Assert.Null(canonical.DeviceId);
        Assert.Equal(canonical.ContentHash, descriptor.CorePayloadHash);
        Assert.Equal(1, await db.Set<FieldTaskStartOrigin>().CountAsync(x => x.TaskId == task));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GenuineReporterFieldStartCannotBeReclassifiedOrRehashedByOfflineAdmission(bool changeKind)
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        var version = Convert.ToBase64String(await db.Defects.Where(x => x.Id == scope.Defect)
            .Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var created = await Command(db, scope, scope.Pm, UserRoleCode.ProjectManager, null, "create",
            new FieldTaskCreateInput(scope.Defect, version, null, "REPORTER", scope.Route, scope.Set, null,
                null, "PRE_MEASUREMENT", 1, "{}", "measure only", scope.Crew, DateTimeOffset.UtcNow.AddDays(1)));
        Assert.Equal(201, created.Status);
        var task = JsonSerializer.SerializeToElement(created.Value, Json).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, task, "accept",
            new FieldTaskActionInput("actual Crew accepts"))).Status);
        var originId = Guid.NewGuid();
        Assert.Equal(201, (await Command(db, scope, scope.Crew, UserRoleCode.RepairCrew, task, "start",
            new FieldStartInput(originId, DateTimeOffset.UtcNow))).Status);
        var origin = await db.Set<FieldInspectionOperationOrigin>().AsNoTracking().SingleAsync(x =>
            x.ProjectId == scope.Project && x.OriginId == originId);
        var beforeCount = await db.Set<FieldTaskStartOrigin>().CountAsync(x => x.TaskId == task);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var rejected = await Assert.ThrowsAsync<OfflineAdmissionRejectedException>(() =>
            new OfflineAdmissionValidator(db, TimeProvider.System).GuardOriginBindingAsync(scope.Project, originId,
                changeKind ? "REPAIR_EXECUTION_START" : "FIELD_START", changeKind ? origin.ContentHash : new string('f', 64),
                scope.Crew, task, CancellationToken.None));
        Assert.Equal(409, rejected.Status); Assert.Equal("origin_content_conflict", rejected.Code);
        Assert.Equal(beforeCount, await db.Set<FieldTaskStartOrigin>().CountAsync(x => x.TaskId == task));
        Assert.Equal(origin.ContentHash, await db.Set<FieldInspectionOperationOrigin>().Where(x => x.Id == origin.Id)
            .Select(x => x.ContentHash).SingleAsync());
        await transaction.RollbackAsync();
    }

    private static async Task<FieldWorkflowResult> Command(RoadGuardDbContext db, Scope scope, Guid actor,
        UserRoleCode role, Guid? task, string action, object input)
    {
        db.ChangeTracker.Clear();
        var expected = task.HasValue ? Convert.ToBase64String(await db.FieldInspectionTasks.Where(x => x.Id == task.Value)
            .Select(x => x.RowVersion).SingleAsync()) : null;
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        return await new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System)
            .ExecuteAsync(new(scope.Project, task, action, input, Guid.NewGuid().ToString(), expected,
                new(actor, role, actor, "DIRECT", true)), async token =>
                await guard.AuthorizeAsync(actor, role, scope.Project, token) is not null, CancellationToken.None);
    }

    private async Task<Scope> Seed()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db); var now = DateTimeOffset.UtcNow;
        ApplicationUser User(UserRoleCode role) => new()
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "offline canonical fixture",
            PasswordHash = "fixture",
            RoleCode = role,
            Status = UserStatus.Active,
            CreatedAt = now
        };
        var pm = User(UserRoleCode.ProjectManager); var crew = User(UserRoleCode.RepairCrew); var reporter = User(UserRoleCode.Reporter);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Offline actual source", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "legacy fixture");
        var line = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new(0, 0), new(20, 0)]);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, line, now, "legacy native fixture");
        var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id);
        var segment = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 1); segment.SetGeometry(0, 20, 0, line);
        var type = DefectType.Create("O" + Guid.NewGuid().ToString("N"), "offline defect");
        db.AddRange(pm, crew, reporter, project, road, route, set, segment, type,
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, new(2000, 1, 1)),
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = crew.Id,
                RoleCode = UserRoleCode.RepairCrew,
                ValidFrom = new(2000, 1, 1),
                Status = ProjectMemberStatus.Active
            });
        await db.SaveChangesAsync();
        var file = StoredFile.Create(Guid.NewGuid(), "private/offline-source-" + Guid.NewGuid().ToString("N"),
            "source.jpg", "image/jpeg", 4, new string('a', 64), reporter.Id, now, null);
        db.AddRange(file, FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter.Id, now)); await db.SaveChangesAsync();
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter.Id, file.StorageUri, "REPORT_PHOTO", "image/jpeg",
            4, file.Checksum, 8388608, now.AddHours(24)); upload.StartUploading("fixture", now);
        db.Add(upload); await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var report = Report.Create(Guid.NewGuid(), reporter.Id, "genuine Reporter without Survey", now,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, Convert.ToBase64String(upload.RowVersion), reporter.Id)]);
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);
        incident.Triage(project.Id, CaseVerificationMethod.ExistingEvidence, "actual retained source", now);
        db.AddRange(report, incident); db.Set<HuyCaseReportLink>().Add(new()
        {
            Id = Guid.NewGuid(),
            CaseId = incident.Id,
            ReportId = report.Id,
            StartedAt = now
        }); await db.SaveChangesAsync();
        var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, report.Id, "fixture-source"),
            project.Id, "fixture-geometry");
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm.Id, source, CandidateDecisionKind.KeepNew,
            CandidateClassification.Create(route.Id, type.Code, null, DefectSeverity.Low, null), null, null, null,
            "actual PM KeepNew", null, CancellationToken.None);
        Assert.NotNull(accepted.DefectId);
        return new(pm.Id, crew.Id, project.Id, route.Id, set.Id, accepted.DefectId.Value);
    }
    private sealed record Scope(Guid Pm, Guid Crew, Guid Project, Guid Route, Guid Set, Guid Defect);
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
