using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Offline;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Offline;
using RoadGuardSystem.Repositories.Projects;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Offline;

public sealed class H5OfflineRepairSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SignedNormalAssessmentStartFinishCommitOriginalCrewAndKeepClaimedTimeUncertain()
    {
        await using var db = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(db, sql);
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await repairs.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, defectVersion, [new("FORMAL_REPAIR", true,
                new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "mandatory repair")], "package"),
            Guid.NewGuid().ToString(), defectVersion), default);
        Assert.Equal(201, created.Status);
        var packageId = Assert.IsType<RepairPackageFact>(created.Value).Id;
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations)
            .SingleAsync(row => row.Id == packageId);
        var proposed = await repairs.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            packageId, new(package.Obligations[0].Id, "NORMAL", "actual plan", "checklist-v1", "proposal"),
            Guid.NewGuid().ToString(), created.Version!), default);
        Assert.Equal(201, proposed.Status);
        var itemId = Assert.IsType<RepairItemFact>(proposed.Value).Id;
        var approved = await repairs.ApproveItemAsync(new(source.Supervisor, UserRoleCode.Supervisor,
            source.Project, packageId, itemId, new("approve plan"), Guid.NewGuid().ToString(),
            proposed.Version!), default);
        Assert.Equal(201, approved.Status);
        var assignment = new RepairItemAssignData(new(source.Defect, defectVersion, null, "REPORTER",
            source.Route, source.Set, null, null, "POST_REPAIR", 1, "{}", null, source.Crew,
            DateTimeOffset.UtcNow.AddDays(1)), null, "assign actual current Crew");
        var assigned = await repairs.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            packageId, itemId, assignment, Guid.NewGuid().ToString(), approved.Version!), default);
        Assert.Equal(201, assigned.Status);
        var binding = Assert.IsType<RepairTaskBindingFact>(assigned.Value);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var direct = new FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        var nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        var accepted = await field.ExecuteAsync(new(source.Project, binding.TaskId, "accept",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("accept"), Guid.NewGuid().ToString(), nativeVersion, direct),
            _ => Task.FromResult(true), default);
        Assert.Equal(201, accepted.Status);
        nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        var started = await field.ExecuteAsync(new(source.Project, binding.TaskId, "start",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow), Guid.NewGuid().ToString(),
            nativeVersion, direct), _ => Task.FromResult(true), default);
        Assert.Equal(201, started.Status);
        var first = await db.Set<FieldTaskStartOrigin>().AsNoTracking()
            .SingleAsync(row => row.TaskId == binding.TaskId);
        var repairBinding = await db.Set<RepairFieldTaskBinding>().AsNoTracking()
            .SingleAsync(row => row.ItemId == itemId && row.TaskId == binding.TaskId);
        var deviceId = Guid.NewGuid();
        using var keys = OfflineDeviceKeys.Generate(source.Crew, deviceId.ToString("D"));
        var registration = OfflineDeviceRegistration.Register(Guid.NewGuid(), source.Project, source.Crew,
            deviceId, 1, UserRoleCode.RepairCrew, keys.PublicKeys.EncryptionPublicKey,
            keys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow);
        db.Add(registration); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        var fieldCore = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db),
            TimeProvider.System, validator, validator);
        var offline = new OfflineWorkflowRepository(db, TimeProvider.System, fieldCore, validator, repairs);
        var scope = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        async Task<bool> Current(CancellationToken token) =>
            await scope.AuthorizeAsync(source.Crew, UserRoleCode.RepairCrew, source.Project, token) is not null;
        var snapshotResult = await offline.ExecuteAsync(new(source.Crew, UserRoleCode.RepairCrew,
            source.Project, "snapshot-create", new OfflineSnapshotData(registration.Id, binding.TaskId),
            null, Guid.NewGuid().ToString(), null), OfflineContractMapping.RepositoryAlgorithms,
            Current, default);
        Assert.Equal(201, snapshotResult.Status);
        var snapshotJson = JsonSerializer.SerializeToElement(snapshotResult.Value, Json);
        var snapshotId = snapshotJson.GetProperty("id").GetGuid();
        var taskVersion = snapshotJson.GetProperty("taskVersion").GetString()!;
        Assert.Equal(itemId, snapshotJson.GetProperty("snapshot").GetProperty("repair")
            .GetProperty("itemId").GetGuid());
        var assessmentId = Guid.NewGuid(); var executionStartId = Guid.NewGuid(); var finishId = Guid.NewGuid();
        OfflineOperationInput Operation(Guid origin, string kind, Guid[] dependencies,
            OfflineRepairPayload repair, DateTimeOffset? claimed = null)
        {
            var value = new OfflineOperationInput(1, origin, origin, kind, source.Crew, deviceId,
                snapshotId, binding.TaskId, repairBinding.AssignmentId, taskVersion,
                new string('0', 64), dependencies, claimed, Repair: repair);
            var hash = kind switch
            {
                "REPAIR_ASSESSMENT" => RepairCoreHash.Assessment(binding.TaskId, itemId, source.Crew,
                    repair.Assessment!),
                "REPAIR_EXECUTION_START" => RepairCoreHash.ExecutionStart(binding.TaskId, itemId, source.Crew,
                    repair.Start!),
                "REPAIR_EXECUTION_FINISH" => RepairCoreHash.ExecutionFinish(binding.TaskId, itemId, source.Crew,
                    repair.Finish!),
                _ => throw new InvalidOperationException("Unexpected repair kind.")
            };
            return value with { CorePayloadHash = hash };
        }
        var assessment = Operation(assessmentId, "REPAIR_ASSESSMENT", [],
            new(itemId, "assessment", Assessment: new(assessmentId, first.Id, null, null, null, deviceId)));
        var executionStart = Operation(executionStartId, "REPAIR_EXECUTION_START", [assessmentId],
            new(itemId, "execution-start", Start: new(executionStartId, first.Id,
                DateTimeOffset.UtcNow.AddHours(-3), assessmentId, deviceId)));
        var finish = Operation(finishId, "REPAIR_EXECUTION_FINISH", [executionStartId],
            new(itemId, "execution-finish", Finish: new(finishId, executionStartId,
                DateTimeOffset.UtcNow.AddHours(-2), deviceId)), DateTimeOffset.UtcNow.AddHours(-2));
        var batchId = Guid.NewGuid();
        var operations = new[] { assessment, executionStart, finish };
        var manifest = OfflineWorkflowEngine.CanonicalManifest(source.Project, batchId, registration.Id,
            operations.Select(OfflineWorkflowEngine.Describe).ToArray());
        var signature = OfflinePackageAuthentication.SignClaim(manifest, keys);
        var result = await offline.ExecuteAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project,
            "sync", OfflineContractMapping.ToData("sync", new OfflineSignedBatchInput(batchId,
                registration.Id, operations, signature)), null, Guid.NewGuid().ToString(), null),
            OfflineContractMapping.RepositoryAlgorithms, Current, default);
        Assert.Equal(200, result.Status);
        var items = JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("items");
        Assert.Equal(3, items.GetArrayLength());
        Assert.All(items.EnumerateArray().ToArray(), item =>
            Assert.True(item.GetProperty("durableAcknowledgment").GetBoolean(), item.GetRawText()));
        Assert.Equal(3, await db.Set<OfflineOperationResult>().CountAsync(row =>
            row.ProjectId == source.Project && row.DurableAck));
        Assert.Equal(source.Crew, (await db.Set<RepairMeasurementAssessment>().AsNoTracking()
            .SingleAsync(row => row.Id == assessmentId)).OriginalActorId);
        var execution = await db.Set<RepairExecutionStart>().AsNoTracking()
            .SingleAsync(row => row.Id == executionStartId);
        var completed = await db.Set<RepairExecutionFinish>().AsNoTracking()
            .SingleAsync(row => row.Id == finishId);
        Assert.Equal(RepairTimeProvenance.Uncertain, execution.TimeProvenance);
        Assert.Equal(RepairTimeProvenance.Uncertain, completed.TimeProvenance);
        Assert.Null(execution.VerifiedOriginalAt);
        Assert.Null(completed.VerifiedOriginalAt);
        Assert.False(await db.Set<RoadGuardSystem.BusinessObjects.Clocks.DeadlineClock>()
            .AnyAsync(row => row.TargetId == itemId && row.Kind ==
                RoadGuardSystem.BusinessObjects.Clocks.DeadlineClockKind.FinishedDataSync));
    }

    [Fact]
    public async Task SignedHandoverAssessmentResolvesRecipientUploadWithoutRewritingDeclaration()
    {
        await using var db = sql.CreateDbContext();
        var ready = await PrepareRepairAsync(db);
        using var sourceKeys = ready.Keys;
        var source = ready.Source;
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        var fieldCore = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db),
            TimeProvider.System, validator, validator);
        var offline = new OfflineWorkflowRepository(db, TimeProvider.System, fieldCore, validator, ready.Repairs);
        var scope = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        Task<OfflineWorkflowFact> Run(Guid actor, UserRoleCode role, string action, object body, Guid? resource = null)
            => offline.ExecuteAsync(new(actor, role, source.Project, action,
                    OfflineContractMapping.ToData(action, body), resource, Guid.NewGuid().ToString(), null),
                OfflineContractMapping.RepositoryAlgorithms,
                async token => await scope.AuthorizeAsync(actor, role, source.Project, token) is not null, default);
        var snapshot = await Run(source.Crew, UserRoleCode.RepairCrew, "snapshot-create",
            new OfflineSnapshotInput(ready.Registration.Id, ready.Binding.TaskId));
        Assert.Equal(201, snapshot.Status);
        var snapshotView = JsonSerializer.SerializeToElement(snapshot.Value, Json);
        var snapshotId = snapshotView.GetProperty("id").GetGuid();
        var taskVersion = snapshotView.GetProperty("taskVersion").GetString()!;
        var recipientDevice = Guid.NewGuid();
        using var recipientKeys = OfflineDeviceKeys.Generate(source.Pm, recipientDevice.ToString("D"));
        var recipient = OfflineDeviceRegistration.Register(Guid.NewGuid(), source.Project, source.Pm,
            recipientDevice, 1, UserRoleCode.ProjectManager,
            recipientKeys.PublicKeys.EncryptionPublicKey, recipientKeys.PublicKeys.SigningPublicKey,
            DateTimeOffset.UtcNow);
        db.Add(recipient); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var now = DateTimeOffset.UtcNow; var origin = Guid.NewGuid(); var captureOrigin = Guid.NewGuid();
        var checksum = new string('c', 64);
        var declaration = new RepairFieldEvidenceData(captureOrigin, null, "BEFORE", checksum,
            "image/jpeg", now, "pre-execution checklist");
        var assessment = new RepairMeasurementAssessmentInput(origin, ready.First.Id, null,
            [new(captureOrigin, null, "BEFORE", checksum, "image/jpeg", now, "pre-execution checklist")],
            null, ready.DeviceId);
        var hash = RepairCoreHash.Assessment(ready.Binding.TaskId, ready.ItemId, source.Crew, assessment);
        var operation = new OfflineOperationInput(1, origin, origin, "REPAIR_ASSESSMENT", source.Crew,
            ready.DeviceId, snapshotId, ready.Binding.TaskId, ready.RepairBinding.AssignmentId,
            taskVersion, hash, [], null, Repair: new(ready.ItemId, "assessment", Assessment: assessment));
        var descriptor = OfflineWorkflowEngine.Describe(operation);
        var sourceBatchId = Guid.NewGuid();
        var signedManifest = OfflineWorkflowEngine.CanonicalManifest(source.Project, sourceBatchId,
            ready.Registration.Id, [descriptor]);
        var sourceSignature = OfflinePackageAuthentication.SignClaim(signedManifest, sourceKeys);
        var attached = OfflineWorkflowEngine.CanonicalAttachedPayload(sourceBatchId, ready.Registration.Id,
            [operation], sourceSignature);
        var attachedHash = Convert.ToHexString(SHA256.HashData(attached)).ToLowerInvariant();
        var packageId = Guid.NewGuid();
        var header = OfflinePackageHeader.Create(packageId, source.Project, source.Crew,
            ready.DeviceId.ToString("D"), [origin], attachedHash);
        var encrypted = OfflineHandoverCrypto.Seal(header, attached, sourceKeys,
            [recipientKeys.PublicKeys]);
        var exported = await Run(source.Crew, UserRoleCode.RepairCrew, "package-export",
            new OfflinePackageExportInput(H5EncryptedPackageDto.FromDomain(encrypted),
                new(sourceBatchId, ready.Registration.Id, [descriptor], sourceSignature)));
        Assert.Equal(201, exported.Status);
        var granted = await Run(source.Supervisor, UserRoleCode.Supervisor, "grant-issue",
            new OfflineHandoverGrantInput(packageId, recipient.Id,
                [new(origin, descriptor.Kind, descriptor.CorePayloadHash, descriptor.EnvelopeHash,
                    ready.Binding.TaskId, ready.RepairBinding.AssignmentId, snapshotId, ready.ItemId)],
                "receive original Crew assessment"));
        Assert.Equal(201, granted.Status);
        var grantId = JsonSerializer.SerializeToElement(granted.Value, Json).GetProperty("id").GetGuid();
        var importBatchId = Guid.NewGuid();
        var endorsement = OfflineRecipientEndorsement.CanonicalClaim(new(source.Project,
            packageId, grantId, importBatchId, recipient.Id, attachedHash));
        var recipientSignature = OfflinePackageAuthentication.SignClaim(endorsement, recipientKeys);
        var imported = new OfflinePackageImportInput(importBatchId, packageId, grantId, recipient.Id,
            [operation], sourceSignature, recipientSignature);

        // Stage exactly the source and recipient signed bytes so upload can precede the first core apply.
        var binding = OfflineOperationBinding.Bind(Guid.NewGuid(), source.Project, origin, origin,
            operation.Kind, hash, descriptor.EnvelopeHash, source.Crew, ready.Registration.Id,
            ready.Binding.TaskId, ready.RepairBinding.AssignmentId, snapshotId,
            JsonSerializer.Serialize(OfflineContractMapping.ToData("sync",
                new OfflineSignedBatchInput(sourceBatchId, ready.Registration.Id,
                    [operation], sourceSignature)) is OfflineSignedBatchData signed
                    ? signed.Operations.Single() : throw new InvalidOperationException(), Json), now, ready.ItemId);
        var batch = OfflineSyncBatch.ReceiveAuthenticated(importBatchId, source.Project,
            sourceBatchId, ready.Registration.Id, source.Pm, packageId, grantId,
            Encoding.UTF8.GetString(signedManifest), sourceSignature, Encoding.UTF8.GetString(attached),
            now, recipient.Id, recipientSignature);
        var admission = OfflineOperationAdmission.Record(Guid.NewGuid(), source.Project, batch.Id,
            binding.Id, source.Pm, UserRoleCode.ProjectManager, grantId,
            JsonSerializer.Serialize(new
            {
                operation.TaskId,
                operation.AssignmentId,
                operation.SnapshotId,
                sourceRegistrationId = ready.Registration.Id,
                claimStatus = "SIGNED_ORIGIN_UNVERIFIED_TIME"
            }, Json), now);
        db.AddRange(binding, batch, admission); await db.SaveChangesAsync(); db.ChangeTracker.Clear();

        var storage = new VerifiedStorage(checksum);
        var uploads = new UploadPersistenceService(db, new IdempotencyOperationService(db), storage,
            Options.Create(new UploadSessionOptions()), TimeProvider.System, validator);
        var upload = new UploadCreatePersistenceRequest(source.Pm, source.Project, ready.Binding.TaskId,
            "BEFORE", "signed-repair-before.jpg", "image/jpeg", 4, checksum, 8388608,
            now.AddHours(24), "repair-upload-" + Guid.NewGuid(), new string('d', 64), origin);
        var request = new OfflineUploadCaptureRequest(source.Project, ready.Binding.TaskId,
            admission.Id, captureOrigin, source.Pm, UserRoleCode.ProjectManager, upload);
        var createdUpload = await uploads.CreateOfflineAsync(request);
        Assert.Equal(UploadPersistenceStatus.Success, createdUpload.Status);
        Assert.NotNull(createdUpload.Session);
        var partAt = DateTimeOffset.UtcNow;
        Assert.Equal(UploadPersistenceStatus.Success, (await uploads.GetPartUrlsAsync(source.Pm,
            source.Project, createdUpload.Session.Id, [1], "repair-part-" + Guid.NewGuid(),
            new string('e', 64), partAt, partAt.AddMinutes(10))).Status);
        var currentUpload = await uploads.GetSessionAsync(createdUpload.Session.Id);
        Assert.NotNull(currentUpload);
        Assert.Equal(UploadPersistenceStatus.Success, (await uploads.CompleteAsync(new(source.Pm,
            source.Project, createdUpload.Session.Id, currentUpload.Version,
            [new CompletedStoragePart(1, "part-etag")], checksum,
            "repair-complete-" + Guid.NewGuid(), new string('f', 64), null))).Status);
        Assert.Equal(UploadPersistenceStatus.Success, await uploads.VerifyNextAsync());
        Assert.Equal(1, await db.Set<OfflineEvidenceCaptureReference>().CountAsync(row =>
            row.AdmissionId == admission.Id && row.ActualUploaderId == source.Pm &&
            row.OriginalActorId == source.Crew));
        (await db.Users.SingleAsync(row => row.Id == source.Crew)).Status = UserStatus.Suspended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await Run(source.Pm, UserRoleCode.ProjectManager, "import", imported);
        Assert.Equal(200, result.Status);
        Assert.True(JsonSerializer.SerializeToElement(result.Value, Json).GetProperty("items")[0]
            .GetProperty("durableAcknowledgment").GetBoolean());
        var retained = await db.Set<RepairMeasurementAssessment>().AsNoTracking()
            .SingleAsync(row => row.Id == origin);
        Assert.Equal(hash, retained.ContentHash);
        Assert.Equal(source.Crew, retained.OriginalActorId);
        Assert.Equal(JsonValueKind.Null, JsonDocument.Parse(retained.PayloadJson).RootElement
            .GetProperty("evidence")[0].GetProperty("fileId").ValueKind);
        var actualEvidence = Assert.Single(retained.Evidence);
        Assert.Equal(createdUpload.Session.FileId, actualEvidence.FileId);
        Assert.Equal(source.Pm, actualEvidence.ActualUploaderId);
        Assert.Equal(source.Crew, actualEvidence.OriginalActorId);
        Assert.Equal(1, await db.Set<OfflineOperationResult>().CountAsync(row =>
            row.OriginId == origin && row.DurableAck));
        Assert.Equal(201, (await Run(source.Supervisor, UserRoleCode.Supervisor, "grant-revoke",
            new OfflineGrantRevokeInput("stop recipient access"), grantId)).Status);
        Assert.Equal(403, (await Run(source.Pm, UserRoleCode.ProjectManager, "import", imported)).Status);
        Assert.False(await uploads.IsCurrentOfflineFileActorAsync(source.Pm, UserRoleCode.ProjectManager,
            createdUpload.Session.FileId));
    }

    private sealed class VerifiedStorage(string checksum) : IUploadObjectStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
            => Task.FromResult("controlled-repair-multipart");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId,
            IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(number =>
                new PresignedUploadPart(number, "https://storage.invalid/repair-part", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId,
            IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default)
            => Task.FromResult(new UploadObjectVerification(4, checksum, "image/jpeg"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("This test does not read object bytes.");
    }

    private sealed record RepairPrepared(H4GenuineRepairSource.Source Source,
        RepairWorkflowRepository Repairs, Guid ItemId, RepairTaskBindingFact Binding,
        RepairFieldTaskBinding RepairBinding, FieldTaskStartOrigin First,
        Guid DeviceId, OfflineDeviceKeys Keys, OfflineDeviceRegistration Registration);

    private async Task<RepairPrepared> PrepareRepairAsync(RoadGuardDbContext db)
    {
        var source = await H4GenuineRepairSource.Seed(db, sql);
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await repairs.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, defectVersion, [new("FORMAL_REPAIR", true,
                new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "mandatory repair")], "package"),
            Guid.NewGuid().ToString(), defectVersion), default);
        Assert.Equal(201, created.Status);
        var packageId = Assert.IsType<RepairPackageFact>(created.Value).Id;
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == packageId);
        var proposed = await repairs.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            packageId, new(package.Obligations[0].Id, "NORMAL", "actual plan", "checklist-v1", "proposal"),
            Guid.NewGuid().ToString(), created.Version!), default);
        Assert.Equal(201, proposed.Status);
        var itemId = Assert.IsType<RepairItemFact>(proposed.Value).Id;
        var approved = await repairs.ApproveItemAsync(new(source.Supervisor, UserRoleCode.Supervisor,
            source.Project, packageId, itemId, new("approve plan"), Guid.NewGuid().ToString(),
            proposed.Version!), default);
        Assert.Equal(201, approved.Status);
        var assignment = new RepairItemAssignData(new(source.Defect, defectVersion, null, "REPORTER",
            source.Route, source.Set, null, null, "POST_REPAIR", 1, "{}", null, source.Crew,
            DateTimeOffset.UtcNow.AddDays(1)), null, "assign actual current Crew");
        var assigned = await repairs.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            packageId, itemId, assignment, Guid.NewGuid().ToString(), approved.Version!), default);
        Assert.Equal(201, assigned.Status);
        var binding = Assert.IsType<RepairTaskBindingFact>(assigned.Value);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var direct = new FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        var nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        Assert.Equal(201, (await field.ExecuteAsync(new(source.Project, binding.TaskId, "accept",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("accept"), Guid.NewGuid().ToString(), nativeVersion, direct),
            _ => Task.FromResult(true), default)).Status);
        nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        Assert.Equal(201, (await field.ExecuteAsync(new(source.Project, binding.TaskId, "start",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow), Guid.NewGuid().ToString(),
            nativeVersion, direct), _ => Task.FromResult(true), default)).Status);
        var first = await db.Set<FieldTaskStartOrigin>().AsNoTracking()
            .SingleAsync(row => row.TaskId == binding.TaskId);
        var repairBinding = await db.Set<RepairFieldTaskBinding>().AsNoTracking()
            .SingleAsync(row => row.ItemId == itemId && row.TaskId == binding.TaskId);
        var deviceId = Guid.NewGuid();
        var keys = OfflineDeviceKeys.Generate(source.Crew, deviceId.ToString("D"));
        var registration = OfflineDeviceRegistration.Register(Guid.NewGuid(), source.Project, source.Crew,
            deviceId, 1, UserRoleCode.RepairCrew, keys.PublicKeys.EncryptionPublicKey,
            keys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow);
        db.Add(registration); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        return new(source, repairs, itemId, binding, repairBinding, first, deviceId, keys, registration);
    }
}
