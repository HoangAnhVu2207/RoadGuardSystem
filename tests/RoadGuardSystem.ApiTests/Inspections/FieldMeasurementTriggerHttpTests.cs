using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.ApiTests.Inspections;

public sealed class FieldMeasurementTriggerHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StandardAndEquivalentPatchedUpgradePreserveReporterAndSurveyGuards(bool equivalentPatch)
    {
        await using var sql = new AuthenticationSqlServerFixture();
        await sql.InitializeAtMigrationAsync("20261117183505_WebPmCoordinationMetadata");
        await using var setup = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(setup, sql);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") });
        var email = await setup.Users.Where(x => x.Id == source.Pm).Select(x => x.Email).SingleAsync();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Json(login)).GetProperty("accessToken").GetString());
        var path = $"/api/v1/projects/{source.Project}/field-inspection-tasks";
        var version = Convert.ToBase64String(await setup.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        object Input(Guid defect, string defectVersion, byte type, string kind = "REPORTER", Guid? survey = null, Guid? routeId = null, Guid? setId = null)
            => new
            {
                defectId = defect,
                defectVersion,
                sourceKind = kind,
                surveyId = survey,
                routeVersionId = routeId ?? source.Route,
                segmentSetId = setId ?? source.Set,
                purpose = "PRE_MEASUREMENT",
                requiredMeasurementType = type,
                measurementScope = "{}",
                assignedToUserId = source.Crew,
                dueAt = DateTimeOffset.UtcNow.AddDays(1)
            };
        async Task<HttpResponseMessage> Post(object body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            return await client.SendAsync(request);
        }
        using var old = await Post(Input(source.Defect, version, 4));
        Assert.Equal(HttpStatusCode.Created, old.StatusCode);
        var oldId = (await Json(old)).GetProperty("id").GetGuid();
        var oldVersion = (await Json(old)).GetProperty("version").GetString();
        if (equivalentPatch)
        {
            // Controlled full definition models only an equivalent 1–6 patch, not any unknown external patch.
            var patch = new RoadGuardSystem.cRepositories.Migrations.FieldTaskMeasurementDimensions();
            await setup.Database.ExecuteSqlRawAsync(Assert.Single(patch.UpOperations.OfType<SqlOperation>()).Sql);
            using var patched = await Post(Input(source.Defect, version, 5));
            Assert.Equal(HttpStatusCode.Created, patched.StatusCode);
        }
        await setup.Database.MigrateAsync();
        var before = (await setup.Database.GetAppliedMigrationsAsync()).ToArray();
        var definition = await setup.Database.SqlQuery<string>($"SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.TR_FieldInspectionTasks_H3Scope')) AS [Value]").SingleAsync();
        Assert.Contains("NOT IN (1,2,3,4,5,6)", definition);
        Assert.Contains("THROW 51132", definition); Assert.Contains("THROW 51133", definition);
        var surveySource = await SurveySource(setup, source);
        var ids = new List<Guid>();
        foreach (byte type in new byte[] { 1, 2, 3, 4, 5, 6 })
            foreach (var variant in new[] { (source.Defect, version, "REPORTER", (Guid?)null), (surveySource.Defect, surveySource.Version, "SURVEY", (Guid?)surveySource.Survey) })
            {
                using var response = await Post(Input(variant.Item1, variant.Item2, type, variant.Item3, variant.Item4));
                Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
                var id = (await Json(response)).GetProperty("id").GetGuid(); ids.Add(id);
                Assert.Equal(type, await setup.FieldInspectionTasks.Where(x => x.Id == id).Select(x => x.RequiredMeasurementType).SingleAsync());
                using var read = await client.GetAsync(path + $"/{id}");
                Assert.Equal(HttpStatusCode.OK, read.StatusCode);
                Assert.Equal(type, (await Json(read)).GetProperty("requiredMeasurementType").GetByte());
            }
        foreach (byte invalid in new byte[] { 0, 7 })
        {
            using var bad = await Post(Input(source.Defect, version, invalid));
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            await using var raw = sql.CreateDbContext();
            var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), "INVALID-" + Guid.NewGuid().ToString("N"), source.Project,
                source.Defect, null, "REPORTER", source.Route, source.Set, null, null, FieldInspectionPurpose.PreMeasurement, 1,
                "{}", null, DateTimeOffset.UtcNow.AddDays(1), source.Pm);
            raw.Add(task); raw.Entry(task).Property(x => x.RequiredMeasurementType).CurrentValue = invalid;
            var rejected = await Assert.ThrowsAsync<DbUpdateException>(() => raw.SaveChangesAsync());
            Assert.Equal(51133, Assert.IsType<SqlException>(rejected.InnerException).Number);
        }
        var immutable = await Assert.ThrowsAsync<SqlException>(() => setup.Database.ExecuteSqlInterpolatedAsync($"UPDATE FieldInspectionTasks SET RequiredMeasurementType=5 WHERE Id={oldId}"));
        Assert.Equal(51132, immutable.Number);
        using var wrongSurvey = await Post(Input(surveySource.Defect, surveySource.Version, 5, "SURVEY", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Conflict, wrongSurvey.StatusCode);
        using var staleSource = await Post(Input(source.Defect, "AAAAAAAAAAA=", 5));
        Assert.Equal(HttpStatusCode.Conflict, staleSource.StatusCode);
        using var wrongRoute = await Post(Input(source.Defect, version, 6, routeId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Conflict, wrongRoute.StatusCode);
        using var wrongSet = await Post(Input(source.Defect, version, 5, setId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Conflict, wrongSet.StatusCode);
        await setup.Database.MigrateAsync();
        Assert.Equal(before, await setup.Database.GetAppliedMigrationsAsync());
        Assert.Equal(oldVersion, Convert.ToBase64String(await setup.FieldInspectionTasks.Where(x => x.Id == oldId).Select(x => x.RowVersion).SingleAsync()));
        Assert.False(setup.Database.HasPendingModelChanges());
        var output = Environment.GetEnvironmentVariable("RG_FIELD_SAFETY_SCHEMA_EVIDENCE");
        if (output is not null) await File.WriteAllTextAsync(output, JsonSerializer.Serialize(await SandboxSeedGuard.ReadSchemaAsync(setup)));
        var downgrade = await Assert.ThrowsAsync<SqlException>(() => setup.GetService<IMigrator>().MigrateAsync("20261117183505_WebPmCoordinationMetadata"));
        Assert.Equal(51990, downgrade.Number);
        Assert.Equal(12, await setup.FieldInspectionTasks.CountAsync(x => ids.Contains(x.Id)));
        await setup.Database.MigrateAsync();
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) { using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return document.RootElement.Clone(); }
    private static async Task<(Guid Defect, Guid Survey, string Version)> SurveySource(RoadGuardSystem.Repositories.RoadGuardDbContext db, GenuineProtectedSource.Source source)
    {
        var now = DateTimeOffset.UtcNow;
        var survey = Survey.Create(Guid.NewGuid(), null, source.Project, source.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Pending, null, null, "[]");
        var block = ProcessingBlock.Create(Guid.NewGuid(), data.Id, 1, "{}");
        var model = AIModelVersion.Create(Guid.NewGuid(), "FIELD fixture", Guid.NewGuid().ToString("N"), "file:///fixture", null, null, AIModelVersionStatus.Draft, null, null);
        var job = ProcessingJob.Create(Guid.NewGuid(), block.Id, model.Id, ProcessingJobStatus.Completed, now.AddMinutes(-1), now, null, null);
        var type = DefectType.Create("FIELD-" + Guid.NewGuid().ToString("N"), "FIELD scope fixture");
        db.AddRange(survey, data, block, model, job, type); await db.SaveChangesAsync();
        var detection = AIDetection.Create(Guid.NewGuid(), job.Id, model.Id, source.Route, null, type.Code, .8m, null, null, "{}");
        db.Add(detection); await db.SaveChangesAsync();
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(source.Pm,
            CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.AiDetection, detection.Id, "persisted-survey-fixture"), source.Project, "fixture-geometry"),
            CandidateDecisionKind.KeepNew, CandidateClassification.Create(source.Route, type.Code, null, DefectSeverity.Low, null), null, null, null, "controlled source fixture", null, default);
        var id = accepted.DefectId!.Value;
        return (id, survey.Id, Convert.ToBase64String(await db.Defects.Where(x => x.Id == id).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync()));
    }
}
