using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

public sealed class Anh01ScopeAdoptionCorrectionTests
{
    [Theory]
    [InlineData("Plan")]
    [InlineData("Request")]
    public async Task Applied_adoption_is_forward_corrected_without_rewriting_JSON_or_refs(string kind)
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            await using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, o => o.UseNetTopologySuite()).Options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261002000100_Anh01M1FileSizeBigint");
            var project = Guid.NewGuid(); var section = Guid.NewGuid(); var route = Guid.NewGuid(); var set = Guid.NewGuid(); var actor = Guid.NewGuid();
            var first = Guid.NewGuid(); var second = Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO Roles (Code,Name,NormalizedName,IsActive) VALUES ('PM','Project manager','PM',1)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (Id,UserName,NormalizedUserName,DisplayName,RoleCode,Status,MustChangePassword,CreatedAt,Email,NormalizedEmail,PasswordHash,SecurityStamp,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount) VALUES ({actor},{actor.ToString()},{actor.ToString().ToUpperInvariant()},{"Isolated fixture"},'PM',1,0,{DateTimeOffset.UtcNow},{"fixture@example.test"},{"FIXTURE@EXAMPLE.TEST"},{"test-only-hash"},{actor.ToString()},0,0,0,0,0)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Projects (Id,ProjectCode,Name,Status,CreatedAt) VALUES ({project},{project.ToString()},{"Scope migration fixture"},1,{DateTimeOffset.UtcNow})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSections (Id,ProjectId,Code) VALUES ({section},{project},'LEGACY')");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSectionVersions (Id,RoadSectionId,VersionNo,IsCurrent,Geometry,EffectiveFrom,ChangeReason) VALUES ({route},{section},1,1,geometry::STGeomFromText('LINESTRING (0 0,200 0)',32648),{DateTimeOffset.UtcNow},'Legacy')");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSegmentSets (Id,RoadSectionVersionId,Status) VALUES ({set},{route},'PUBLISHED')");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RoadSegments (Id,SegmentSetId,RoadSectionVersionId,Sequence) VALUES ({first},{set},{route},1),({second},{set},{route},2)");
            JsonObject Tuple(string band) => new() { ["routeVersionId"] = route.ToString(), ["segmentSetId"] = set.ToString(), ["targetBand"] = band, ["segmentIds"] = new JsonArray(first.ToString(), second.ToString()) };
            JsonArray Correct() => new(Tuple("SURFACE"), Tuple("RIGHT_EDGE"));
            var cases = new List<(string Name, string Snapshot, string RelIds, bool Valid)>();
            var normalIds = new JsonArray(first.ToString(), second.ToString()).ToJsonString();
            cases.Add(("valid", Correct().ToJsonString(), normalIds, true));
            cases.Add(("duplicate-tuples", new JsonArray(Tuple("SURFACE"), Tuple("SURFACE")).ToJsonString(), normalIds, false));
            var reordered = Correct(); reordered[0]!["segmentIds"] = new JsonArray(second.ToString(), first.ToString());
            cases.Add(("valid-reordered", reordered.ToJsonString(), normalIds, true));
            foreach (var name in new[] { "duplicate-ids", "object-ids", "invalid-id", "numeric-id", "missing-field", "scalar-tuple", "empty-ids", "missing-id", "extra-id", "overlong-id", "overlong-route", "snapshot-band-space", "id-space", "route-space" })
            {
                var value = Correct();
                switch (name)
                {
                    case "duplicate-ids": value[0]!["segmentIds"] = new JsonArray(first.ToString(), second.ToString(), first.ToString()); break;
                    case "object-ids": value[0]!["segmentIds"] = new JsonObject { ["a"] = first.ToString(), ["b"] = second.ToString() }; break;
                    case "invalid-id": value[0]!["segmentIds"] = new JsonArray("bad", second.ToString()); break;
                    case "numeric-id": value[0]!["segmentIds"] = new JsonArray(1, 2); break;
                    case "missing-field": ((JsonObject)value[0]!).Remove("segmentSetId"); break;
                    case "scalar-tuple": value[0] = "bad"; break;
                    case "empty-ids": value[0]!["segmentIds"] = new JsonArray(); break;
                    case "missing-id": value[0]!["segmentIds"] = new JsonArray(first.ToString()); break;
                    case "extra-id": value[0]!["segmentIds"] = new JsonArray(first.ToString(), second.ToString(), Guid.NewGuid().ToString()); break;
                    case "overlong-id": value[0]!["segmentIds"] = new JsonArray(first.ToString() + "junk", second.ToString()); break;
                    case "overlong-route": value[0]!["routeVersionId"] = route.ToString() + "junk"; break;
                    case "snapshot-band-space": value[0]!["targetBand"] = "SURFACE "; break;
                    case "id-space": value[0]!["segmentIds"] = new JsonArray(first.ToString() + " ", second.ToString()); break;
                    case "route-space": value[0]!["routeVersionId"] = route.ToString() + " "; break;
                }
                cases.Add((name, value.ToJsonString(), normalIds, false));
            }
            cases.Add(("relational-duplicate", Correct().ToJsonString(), new JsonArray(first.ToString(), second.ToString(), first.ToString()).ToJsonString(), false));
            cases.Add(("relational-object", Correct().ToJsonString(), new JsonObject { ["a"] = first.ToString(), ["b"] = second.ToString() }.ToJsonString(), false));
            cases.Add(("relational-band-space", Correct().ToJsonString(), normalIds, false));
            var duplicateProperty = Correct().ToJsonString().Replace("\"targetBand\":", "\"targetBand\":\"SURFACE\",\"targetBand\":");
            // Requests retain duplicate properties verbatim below rather than reparsing through JsonNode.
            cases.Add(("duplicate-property", duplicateProperty, normalIds, false));
            cases.Add(("legacy", "{\"formats\":[\"video\"]}", normalIds, false));
            if (kind == "Request")
            {
                // Raw strings retain duplicate root keys; JsonNode would erase the ambiguity.
                var valid = Correct().ToJsonString();
                cases.Add(("root-duplicate-first-valid", "{\"scope\":" + valid + ",\"scope\":[]}", normalIds, false));
                cases.Add(("root-duplicate-last-valid", "{\"scope\":[],\"scope\":" + valid + "}", normalIds, false));
                cases.Add(("root-duplicate-identical", "{\"scope\":" + valid + ",\"scope\":" + valid + "}", normalIds, false));
                cases.Add(("root-wrapper-valid", "{\"accessPoint\":{\"note\":\"fixture\"},\"scope\":" + valid + "}", normalIds, true));
                cases.Add(("root-whitespace-valid", "\r\n\t {\"scope\":" + valid + "}", normalIds, true));
                cases.Add(("root-wrong-case", "{\"Scope\":" + valid + "}", normalIds, false));
                cases.Add(("root-object-scope", "{\"scope\":{\"items\":" + valid + "}}", normalIds, false));
                cases.Add(("root-array", "[{\"scope\":" + valid + "}]", normalIds, false));
            }
            var rows = new List<(Guid Id, string Name, string Output, bool Valid)>();
            foreach (var item in cases)
            {
                var id = Guid.NewGuid();
                var output = kind == "Plan" || item.Name.StartsWith("root-", StringComparison.Ordinal) ? item.Snapshot : "{\"scope\":" + item.Snapshot + "}";
                if (kind == "Plan")
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SurveyPlans (Id,ProjectId,RoadSectionId,RoadSectionVersionId,PlannedStartAt,PlannedEndAt,SurveyType,Status,OutputRequirements) VALUES ({id},{project},{section},{route},{DateTimeOffset.UtcNow},{DateTimeOffset.UtcNow.AddHours(1)},1,4,{output})");
                else
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SurveyRequests (Id,ProjectId,RoadSectionId,RoadSectionVersionId,RequestedByUserId,RequestedAt,DueAt,SurveyType,Status,OutputRequirements) VALUES ({id},{project},{section},{route},{actor},{DateTimeOffset.UtcNow},{DateTimeOffset.UtcNow.AddDays(1)},1,1,{output})");
                foreach (var band in new[] { "SURFACE", "RIGHT_EDGE" })
                {
                    var storedBand = item.Name == "relational-band-space" ? band + " " : band;
                    if (kind == "Plan")
                        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SurveyPlanScopes (Id,SurveyPlanId,RouteSectionVersionId,SegmentSetId,TargetBand,SegmentIdsJson) VALUES ({Guid.NewGuid()},{id},{route},{set},{storedBand},{item.RelIds})");
                    else
                        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SurveyRequestScopes (Id,SurveyRequestId,RouteSectionVersionId,SegmentSetId,TargetBand,SegmentIdsJson) VALUES ({Guid.NewGuid()},{id},{route},{set},{storedBand},{item.RelIds})");
                }
                rows.Add((id, item.Name, output, item.Valid));
            }
            await migrator.MigrateAsync("20261002031518_Anh01GeometrySurveyReview");
            var adopted = kind == "Plan" ? await db.SurveyPlans.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.ScopeFormatVersion)
                : await db.SurveyRequests.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.ScopeFormatVersion);
            adopted[rows.Single(x => x.Name == "duplicate-tuples").Id].Should().Be("BAND_V1", "reproduce the already-applied old migration before forward correction");
            var beforeScopeIds = kind == "Plan" ? await db.SurveyPlanScopes.AsNoTracking().Select(x => x.Id).ToArrayAsync()
                : await db.SurveyRequestScopes.AsNoTracking().Select(x => x.Id).ToArrayAsync();
            await migrator.MigrateAsync("20261002090000_Anh01ScopeAdoptionCorrection");
            if (kind == "Request")
            {
                var oldAdoption = await db.SurveyRequests.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.ScopeFormatVersion);
                oldAdoption[rows.Single(x => x.Name == "root-duplicate-first-valid").Id].Should().Be("BAND_V1", "the applied correction reads the first scope key");
                oldAdoption[rows.Single(x => x.Name == "root-duplicate-identical").Id].Should().Be("BAND_V1", "duplicates are ambiguous even with identical values");
                oldAdoption[rows.Single(x => x.Name == "root-duplicate-last-valid").Id].Should().BeNull("the first empty scope is not adopted, despite the runtime reading the valid last scope");
                using var document = System.Text.Json.JsonDocument.Parse(rows.Single(x => x.Name == "root-duplicate-first-valid").Output);
                document.RootElement.GetProperty("scope").GetArrayLength().Should().Be(0, "the runtime reader selects the last duplicate key");
            }
            await migrator.MigrateAsync();
            var corrected = kind == "Plan" ? await db.SurveyPlans.AsNoTracking().ToDictionaryAsync(x => x.Id, x => new { x.ScopeFormatVersion, x.OutputRequirements })
                : await db.SurveyRequests.AsNoTracking().ToDictionaryAsync(x => x.Id, x => new { x.ScopeFormatVersion, x.OutputRequirements });
            foreach (var row in rows)
            {
                corrected[row.Id].ScopeFormatVersion.Should().Be(row.Valid ? "BAND_V1" : null, row.Name);
                corrected[row.Id].OutputRequirements.Should().Be(row.Output, "snapshots are never rewritten");
            }
            var afterScopeIds = kind == "Plan" ? await db.SurveyPlanScopes.AsNoTracking().Select(x => x.Id).ToArrayAsync()
                : await db.SurveyRequestScopes.AsNoTracking().Select(x => x.Id).ToArrayAsync();
            afterScopeIds.Should().BeEquivalentTo(beforeScopeIds);
            (await db.IdempotencyRecords.CountAsync()).Should().Be(0);
            var down = () => migrator.MigrateAsync("20261002031518_Anh01GeometrySurveyReview");
            (await down.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(51027);
            (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002090000_Anh01ScopeAdoptionCorrection");
            (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002100000_Anh01RequestScopeRootCorrection");
        }
        finally { await fixture.DisposeAsync(); }
    }
}
