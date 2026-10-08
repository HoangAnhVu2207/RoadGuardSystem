using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

public sealed class Anh01LegacyGeometryCorrectionTests
{
    [Fact]
    public async Task Migration_preserves_legacy_refs_and_HTTP_reads_incomplete_without_fabrication()
    {
        var sql = new AuthenticationSqlServerFixture();
        await sql.InitializeAsync();
        try
        {
            var pm = await sql.CreateUserAsync($"legacy-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
            var project = Guid.NewGuid(); var section = Guid.NewGuid(); var route = Guid.NewGuid(); var set = Guid.NewGuid(); var segment = Guid.NewGuid();
            await using (var db = sql.CreateDbContext())
            {
                var migrator = db.GetService<IMigrator>();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Projects (Id,ProjectCode,Name,Status,CreatedAt,EngineeringUtmSrid) VALUES ({project},{project.ToString()},{"Legacy geometry"},1,{DateTimeOffset.UtcNow},32648)");
                db.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project, UserId = pm.Id, RoleCode = UserRoleCode.ProjectManager, IsPrimary = true, ValidFrom = new DateOnly(2026, 1, 1), Status = ProjectMemberStatus.Active });
                await db.SaveChangesAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSections (Id,ProjectId,Code,Name) VALUES ({section},{project},{"LEGACY"},{"Original road"})");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSectionVersions (Id,RoadSectionId,VersionNo,IsCurrent,Geometry,EffectiveFrom,ChangeReason) VALUES ({route},{section},1,1,geometry::STGeomFromText('LINESTRING (500000 1200000,500200 1200000)',32648),{DateTimeOffset.UtcNow},{"Original measured centerline"})");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSegmentSets (Id,RoadSectionVersionId,Status) VALUES ({set},{route},{"PUBLISHED"})");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSegments (Id,SegmentSetId,RoadSectionVersionId,Sequence) VALUES ({segment},{set},{route},1)");
                await migrator.MigrateAsync();

            }
            await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
            using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(pm.UserName!), password = "Current1!" });
            login.EnsureSuccessStatusCode();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            var prefix = $"/api/v1/projects/{project}";
            var sets = $"{prefix}/road-sections/{section}/versions/{route}/segment-sets";
            var package = await client.GetAsync($"{prefix}/geometry-package?routeVersionId={route}&segmentSetId={set}");
            package.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await package.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("route").GetProperty("metadataStatus").GetString().Should().Be("LEGACY_INCOMPLETE");
            body.GetProperty("route").GetProperty("stationOriginMeters").ValueKind.Should().Be(JsonValueKind.Null);
            body.GetProperty("route").GetProperty("lengthMeters").GetDouble().Should().Be(200);
            var setBody = body.GetProperty("segmentSet");
            setBody.GetProperty("id").GetGuid().Should().Be(set);
            setBody.GetProperty("metadataStatus").GetString().Should().Be("LEGACY_INCOMPLETE");
            setBody.GetProperty("missingMetadata").EnumerateArray().Select(x => x.GetString()).Should().Contain("definition");
            setBody.GetProperty("definition").ValueKind.Should().Be(JsonValueKind.Null);
            var child = setBody.GetProperty("segments")[0];
            child.GetProperty("id").GetGuid().Should().Be(segment);
            child.GetProperty("sequence").GetInt32().Should().Be(1);
            foreach (var field in new[] { "fromOffsetMeters", "toOffsetMeters", "startStationMeters", "endStationMeters", "lengthMeters", "metricGeometry" })
                child.GetProperty(field).ValueKind.Should().Be(JsonValueKind.Null);
            (await client.GetAsync(sets + $"/{set}")).StatusCode.Should().Be(HttpStatusCode.OK);
            // Read does not backfill. The unchanged command admission still rejects incomplete geometry.
            var preview = await client.PostAsJsonAsync(sets + "/preview", new { targetLengthMeters = 100d, remainderMode = "KEEP" });
            preview.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            (await preview.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("geometry_metadata_incomplete");
            await using var verified = sql.CreateDbContext();
            var row = await verified.RoadSegments.AsNoTracking().SingleAsync(x => x.Id == segment);
            row.Geometry.Should().BeNull(); row.FromOffsetMeters.Should().BeNull(); row.ToOffsetMeters.Should().BeNull();
            (await verified.RoadSegmentSets.AsNoTracking().SingleAsync(x => x.Id == set)).DefinitionJson.Should().BeNull();
            (await verified.Set<RoadGeometryMetadata>().CountAsync(x => x.RoadSectionVersionId == route)).Should().Be(0);
            (await verified.IdempotencyRecords.CountAsync(x => x.ProjectId == project)).Should().Be(0);
        }
        finally { await sql.DisposeAsync(); }
    }
}
