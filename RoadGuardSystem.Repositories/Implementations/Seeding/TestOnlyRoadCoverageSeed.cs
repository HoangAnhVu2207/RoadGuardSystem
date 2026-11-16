using System.Data;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>SQL-only synthetic metadata for exercising Supervisor source selection in development.</summary>
public static class TestOnlyRoadCoverageSeed
{
    public static Task EnsureAsync(RoadGuardDbContext db, Guid project, string number, Guid actor,
        DateTimeOffset at, CancellationToken token, Guid? fixtureDefect = null, Guid? fixtureRoad = null)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await Anh02ReceiptAuthority.LockAsync(db, actor, project, token);
            var document = await db.HandoverDocuments.SingleOrDefaultAsync(row => row.ProjectId == project && row.DocumentNo == number, token);
            if (document is null)
            {
                await transaction.CommitAsync(token);
                return;
            }

            async Task<Guid> AddFixtureFile(string name)
            {
                var storageKey = $"TEST_ONLY/{project:N}/road-coverage/{name}";
                var existing = await db.Files.AsNoTracking().SingleOrDefaultAsync(row => row.StorageUri == storageKey, token);
                if (existing is not null)
                {
                    var valid = await db.FileScopes.AnyAsync(row => row.ProjectId == project && row.TargetId == project &&
                        row.FileId == existing.Id && row.Purpose == "TEST_ONLY_COVERAGE", token) &&
                        await db.UploadSessions.AnyAsync(row => row.FileId == existing.Id &&
                            row.Status == aBusinessObjects.Commons.UploadSessionStatus.Verified, token);
                    if (!valid) throw new InvalidOperationException("Existing TEST_ONLY road coverage fixture has unexpected scope or status.");
                    return existing.Id;
                }

                const string purpose = "TEST_ONLY_COVERAGE";
                var file = StoredFile.Create(Guid.NewGuid(), storageKey, name, "application/pdf", 4, new string('a', 64), actor, at, null);
                var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor, storageKey, purpose,
                    file.MimeType, file.SizeBytes, file.Checksum, 8 * 1024 * 1024, at.AddDays(1));
                upload.StartUploading("TEST_ONLY", at);
                db.AddRange(file, upload, FileScope.Create(Guid.NewGuid(), file.Id, project, project, actor, purpose, at));
                await db.SaveChangesAsync(token);
                upload.StartVerification(Convert.ToBase64String(upload.RowVersion), at);
                await db.SaveChangesAsync(token);
                upload.MarkVerified();
                await db.SaveChangesAsync(token);
                return file.Id;
            }

            if (document.FileId is null && !await db.Set<RoadCoverageMapping>().AnyAsync(row => row.HandoverDocumentId == document.Id, token))
            {
                document.FileId = await AddFixtureFile("handover.pdf");
                document.AcceptedByUserId ??= actor;
            }
            await AddFixtureFile("maintenance-basis.pdf");
            if (fixtureDefect is Guid defect && fixtureRoad is Guid road &&
                !await db.RepairPackages.AnyAsync(row => row.DefectId == defect, token))
            {
                var scope = RepairActualScope.Create(Guid.NewGuid(), road, "TEST_ONLY_POSTMAN_V1", "TEST_ONLY Postman road", 1, 2, 0, 1);
                var obligation = RepairObligation.Create(Guid.NewGuid(), project, defect, RepairObligationKind.FormalRepair, true, scope);
                db.RepairPackages.Add(RepairPackage.Create(Guid.NewGuid(), project, defect, [obligation]));
            }
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        });
}
