using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class StaffMembershipReadHttpTests(AuthenticationSqlServerFixture sql)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

    [Fact]
    public async Task SupervisorDirectoryExcludesReporterByDefaultAndContactDetailStaysPrivate()
    {
        var prefix = "STAFF-" + Guid.NewGuid().ToString("N");
        var sup = await User(UserRoleCode.Supervisor, prefix + "-sup");
        var pm = await User(UserRoleCode.ProjectManager, prefix + "-pm");
        var reporter = await User(UserRoleCode.Reporter, prefix + "-reporter");
        await using (var db = sql.CreateDbContext())
        {
            await db.Users.Where(u => u.Id == pm.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.PhoneNumber, "000-test-contact"));
            await db.Users.Where(u => u.Id == reporter.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, UserStatus.Suspended));
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup);
        var page = await Get(client, "/api/v1/users?search=" + prefix);
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.DoesNotContain(page.GetProperty("items").EnumerateArray(), x => x.GetProperty("userId").GetGuid() == reporter.Id);
        var explicitReporter = await Get(client, "/api/v1/users?role=REPORTER&status=SUSPENDED&search=" + prefix);
        Assert.Equal(reporter.Id, Assert.Single(explicitReporter.GetProperty("items").EnumerateArray()).GetProperty("userId").GetGuid());
        var directoryPm = Assert.Single((await Get(client, "/api/v1/users?role=PM&search=" + pm.Email)).GetProperty("items").EnumerateArray());
        Assert.Equal(pm.Email, directoryPm.GetProperty("email").GetString());
        Assert.Equal("000-test-contact", directoryPm.GetProperty("phoneNumber").GetString());
        Assert.Equal(JsonValueKind.Null, Assert.Single(explicitReporter.GetProperty("items").EnumerateArray()).GetProperty("phoneNumber").ValueKind);
        var detailResponse = await client.GetAsync($"/api/v1/users/{pm.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.NotNull(detailResponse.Headers.ETag);
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(pm.Id, detail.GetProperty("id").GetGuid());
        Assert.Equal(new[] { "displayName", "id", "role", "version" }, detail.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
        Assert.Equal(pm.DisplayName, detail.GetProperty("displayName").GetString());
        Assert.Equal("PM", detail.GetProperty("role").GetString());
        Assert.Equal('"' + detail.GetProperty("version").GetString() + '"', detailResponse.Headers.ETag!.ToString());
        using var pmClient = await Login(factory, pm);
        Assert.Equal(HttpStatusCode.Forbidden, (await pmClient.GetAsync($"/api/v1/users/{pm.Id}")).StatusCode);
        var me = await Get(pmClient, "/api/v1/me");
        Assert.False(me.TryGetProperty("phoneNumber", out _));
        Assert.False(me.TryGetProperty("email", out _));
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Reporter)]
    public async Task UnsupportedRolesCannotReadDirectoryOrContact(UserRoleCode role)
    {
        var actor = await User(role);
        var project = await Project();
        await Member(project.Id, actor.Id, role);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, actor);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/users?role=REPORTER")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/users/{actor.Id}")).StatusCode);
        if (role != UserRoleCode.ProjectManager)
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/projects/{project.Id}/members")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/projects/{project.Id}/primary-project-manager")).StatusCode);
        }
    }

    [Fact]
    public async Task RosterFiltersCurrentFactsAndHistoryPreservesMembershipIdentityAndVersions()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager);
        var crew = await User(UserRoleCode.RepairCrew);
        var suspended = await User(UserRoleCode.DroneOperator);
        var mismatch = await User(UserRoleCode.Reporter);
        var project = await Project();
        var pmMember = await Member(project.Id, pm.Id, UserRoleCode.ProjectManager); // non-primary PM may read
        var crewMember = await Member(project.Id, crew.Id, UserRoleCode.RepairCrew, until: Today);
        await Member(project.Id, crew.Id, UserRoleCode.RepairCrew, from: Today.AddDays(1));
        await Member(project.Id, crew.Id, UserRoleCode.RepairCrew, status: ProjectMemberStatus.Ended);
        await Member(project.Id, crew.Id, UserRoleCode.RepairCrew, until: Today.AddDays(-1));
        await Member(project.Id, suspended.Id, UserRoleCode.DroneOperator);
        await Member(project.Id, mismatch.Id, UserRoleCode.RepairCrew);
        await using (var db = sql.CreateDbContext()) await db.Users.Where(u => u.Id == suspended.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, UserStatus.Suspended));
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup);
        using var pmClient = await Login(factory, pm);
        var path = $"/api/v1/projects/{project.Id}/members";
        var current = await Get(client, path);
        Assert.Equal(2, current.GetProperty("items").GetArrayLength());
        Assert.Equal(2, (await Get(pmClient, path)).GetProperty("items").GetArrayLength());
        var member = Assert.Single(current.GetProperty("items").EnumerateArray(), x => x.GetProperty("membershipId").GetGuid() == crewMember.Id);
        Assert.Equal(Convert.ToBase64String(crewMember.RowVersion), member.GetProperty("membershipVersion").GetString());
        Assert.False(member.TryGetProperty("email", out _));
        Assert.False(member.TryGetProperty("phoneNumber", out _));
        var history = await Get(client, path + "?includeHistory=true");
        Assert.Equal(7, history.GetProperty("items").GetArrayLength());
        await using (var db = sql.CreateDbContext())
        {
            var versions = await db.ProjectMembers.Where(x => x.ProjectId == project.Id).ToDictionaryAsync(x => x.Id, x => x.RowVersion);
            foreach (var row in history.GetProperty("items").EnumerateArray())
                Assert.Equal(Convert.ToBase64String(versions[row.GetProperty("membershipId").GetGuid()]), row.GetProperty("membershipVersion").GetString());
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await pmClient.GetAsync(path + "?includeHistory=true")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + "?status=ENDED")).StatusCode);
        Assert.Single((await Get(client, path + "?includeHistory=true&status=ENDED")).GetProperty("items").EnumerateArray());
        Assert.Empty((await Get(client, path + "?search=" + Uri.EscapeDataString(crew.Email!))).GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, (await Get(client, $"/api/v1/projects/{project.Id}/primary-project-manager")).GetProperty("primaryProjectManager").ValueKind);
        Assert.Equal(pmMember.Id, Assert.Single(current.GetProperty("items").EnumerateArray(), x => x.GetProperty("userId").GetGuid() == pm.Id).GetProperty("membershipId").GetGuid());
    }

    [Fact]
    public async Task PmScopeUsesCurrentEffectiveMembershipAndCurrentAccountOnEveryRequest()
    {
        var pm = await User(UserRoleCode.ProjectManager);
        var project = await Project(); var other = await Project();
        var membership = await Member(project.Id, pm.Id, UserRoleCode.ProjectManager, from: Today, until: Today);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, pm);
        foreach (var surface in new[] { "members", "primary-project-manager" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/projects/{project.Id}/{surface}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/projects/{other.Id}/{surface}")).StatusCode);
        }
        foreach (var condition in new[] { "future", "expired", "ended", "wrong-role" })
        {
            await using (var db = sql.CreateDbContext())
            {
                var row = await db.ProjectMembers.SingleAsync(x => x.Id == membership.Id);
                row.ValidFrom = condition == "future" ? Today.AddDays(1) : Today.AddDays(-2);
                row.ValidTo = condition == "expired" ? Today.AddDays(-1) : null;
                row.Status = condition == "ended" ? ProjectMemberStatus.Ended : ProjectMemberStatus.Active;
                row.RoleCode = condition == "wrong-role" ? UserRoleCode.RepairCrew : UserRoleCode.ProjectManager;
                await db.SaveChangesAsync();
            }
            foreach (var surface in new[] { "members", "primary-project-manager" })
                Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/projects/{project.Id}/{surface}")).StatusCode);
        }
        await using (var db = sql.CreateDbContext()) await db.Users.Where(x => x.Id == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.Status, UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/projects/{project.Id}/members")).StatusCode);
    }

    [Fact]
    public async Task PrimaryPmReadVersionDrivesExistingReassignmentAndStalePutConflicts()
    {
        var prefix = "CHAIN-" + Guid.NewGuid().ToString("N");
        var sup = await User(UserRoleCode.Supervisor);
        var oldPm = await User(UserRoleCode.ProjectManager, prefix + "-old");
        var newPm = await User(UserRoleCode.ProjectManager, prefix + "-new");
        var project = await Project();
        var old = await Member(project.Id, oldPm.Id, UserRoleCode.ProjectManager, primary: true);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup);
        Assert.Equal(project.Id, Assert.Single((await Get(client, "/api/v1/projects?search=" + project.ProjectCode)).GetProperty("items").EnumerateArray()).GetProperty("projectId").GetGuid());
        var selected = Assert.Single((await Get(client, "/api/v1/users?role=PM&status=ACTIVE&search=" + prefix + "-new")).GetProperty("items").EnumerateArray());
        var url = $"/api/v1/projects/{project.Id}/primary-project-manager";
        var head = (await Get(client, url)).GetProperty("primaryProjectManager");
        var version = head.GetProperty("membershipVersion").GetString();
        Assert.Equal(old.Id, head.GetProperty("membershipId").GetGuid());
        Assert.Equal(Convert.ToBase64String(old.RowVersion), version);
        var roster = Assert.Single((await Get(client, $"/api/v1/projects/{project.Id}/members?role=PM")).GetProperty("items").EnumerateArray());
        Assert.Equal(version, roster.GetProperty("membershipVersion").GetString());
        // Simulate an intervening persisted change to the same valid head, retaining all command preconditions.
        await using (var db = sql.CreateDbContext())
        {
            var row = await db.ProjectMembers.SingleAsync(x => x.Id == old.Id);
            row.ValidTo = Today; await db.SaveChangesAsync();
        }
        var stale = await client.PutAsJsonAsync(url, new { primaryProjectManagerUserId = newPm.Id, effectiveFrom = Today, reason = "Stale fixture head", expectedCurrentMembershipRowVersion = version, operationId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var refreshed = (await Get(client, url)).GetProperty("primaryProjectManager").GetProperty("membershipVersion").GetString();
        Assert.NotEqual(version, refreshed);
        var response = await client.PutAsJsonAsync(url, new { primaryProjectManagerUserId = selected.GetProperty("userId").GetGuid(), effectiveFrom = Today, reason = "Fixture chain", expectedCurrentMembershipRowVersion = refreshed, operationId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var newHead = (await Get(client, url)).GetProperty("primaryProjectManager");
        Assert.Equal(newPm.Id, newHead.GetProperty("userId").GetGuid());
        Assert.NotEqual(version, newHead.GetProperty("membershipVersion").GetString());
    }

    [Fact]
    public async Task DirectoryIncludesNonActiveStaffAndCurrentRoleRevocationDeniesOldSession()
    {
        var prefix = "STATUS-" + Guid.NewGuid().ToString("N");
        var sup = await User(UserRoleCode.Supervisor);
        var staff = await User(UserRoleCode.RepairCrew, prefix);
        await using (var db = sql.CreateDbContext()) await db.Users.Where(x => x.Id == staff.Id).ExecuteUpdateAsync(x =>
            x.SetProperty(u => u.Status, UserStatus.Suspended).SetProperty(u => u.Email, (string?)null));
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup);
        var row = Assert.Single((await Get(client, "/api/v1/users?search=" + prefix)).GetProperty("items").EnumerateArray());
        Assert.Equal("SUSPENDED", row.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("email").ValueKind);
        var detail = await Get(client, $"/api/v1/users/{staff.Id}");
        Assert.False(detail.TryGetProperty("phoneNumber", out _));
        Assert.False(detail.TryGetProperty("email", out _));
        Assert.Empty((await Get(client, "/api/v1/users?status=ACTIVE&search=" + prefix)).GetProperty("items").EnumerateArray());
        await using (var db = sql.CreateDbContext()) await db.Users.Where(x => x.Id == sup.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.RoleCode, UserRoleCode.ProjectManager));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/users")).StatusCode);
    }

    [Fact]
    public async Task ScopedPmUsesActualCookieForProjectReadsWithoutContactOrHistoryAccess()
    {
        var pm = await User(UserRoleCode.ProjectManager); var project = await Project();
        await Member(project.Id, pm.Id, UserRoleCode.ProjectManager, primary: true, from: Today, until: Today);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var cookie = await Login(factory, pm, cookie: true);
        Assert.Null(cookie.DefaultRequestHeaders.Authorization);
        var primary = (await Get(cookie, $"/api/v1/projects/{project.Id}/primary-project-manager")).GetProperty("primaryProjectManager");
        Assert.Equal(pm.Id, primary.GetProperty("userId").GetGuid());
        Assert.False(primary.TryGetProperty("email", out _));
        Assert.Single((await Get(cookie, $"/api/v1/projects/{project.Id}/members")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await cookie.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cookie.GetAsync($"/api/v1/projects/{project.Id}/members?includeHistory=true")).StatusCode);
        await using (var db = sql.CreateDbContext()) await db.Users.Where(x => x.Id == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.Status, UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.GetAsync($"/api/v1/projects/{project.Id}/members")).StatusCode);
    }

    [Theory]
    [InlineData("future")]
    [InlineData("expired")]
    [InlineData("ended")]
    [InlineData("suspended")]
    [InlineData("role-mismatch")]
    public async Task InvalidPrimaryNeverFallsBackToStaleAccountOrMembership(string condition)
    {
        var sup = await User(UserRoleCode.Supervisor); var pm = await User(UserRoleCode.ProjectManager);
        var project = await Project();
        await Member(project.Id, pm.Id, UserRoleCode.ProjectManager, primary: true,
            from: condition == "future" ? Today.AddDays(1) : Today.AddDays(-2),
            until: condition == "expired" ? Today.AddDays(-1) : null,
            status: condition == "ended" ? ProjectMemberStatus.Ended : ProjectMemberStatus.Active);
        await using (var db = sql.CreateDbContext())
        {
            if (condition == "suspended") await db.Users.Where(x => x.Id == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.Status, UserStatus.Suspended));
            if (condition == "role-mismatch") await db.Users.Where(x => x.Id == pm.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.RoleCode, UserRoleCode.RepairCrew));
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup);
        Assert.Equal(JsonValueKind.Null, (await Get(client, $"/api/v1/projects/{project.Id}/primary-project-manager")).GetProperty("primaryProjectManager").ValueKind);
        Assert.Empty((await Get(client, $"/api/v1/projects/{project.Id}/members")).GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task CursorPagingIsStableFilterBoundAndValidatedBeforeSqlPaging()
    {
        var prefix = "PAGE-" + Guid.NewGuid().ToString("N");
        var sup = await User(UserRoleCode.Supervisor);
        var project = await Project();
        foreach (var suffix in new[] { "a", "b", "c" })
        {
            var user = await User(UserRoleCode.RepairCrew, prefix + suffix);
            await Member(project.Id, user.Id, UserRoleCode.RepairCrew);
            await Member(project.Id, user.Id, UserRoleCode.RepairCrew, status: ProjectMemberStatus.Ended);
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup);
        foreach (var path in new[] { "/api/v1/users?search=" + prefix + "&role=CREW", $"/api/v1/projects/{project.Id}/members?role=CREW" })
        {
            var first = await Get(client, path + "&limit=1");
            var cursor = first.GetProperty("nextCursor").GetString(); Assert.False(string.IsNullOrWhiteSpace(cursor));
            var ids = new HashSet<Guid>();
            var page = first;
            do
            {
                foreach (var row in page.GetProperty("items").EnumerateArray()) Assert.True(ids.Add(row.GetProperty("userId").GetGuid()));
                cursor = page.GetProperty("nextCursor").GetString();
                if (cursor is null) break;
                page = await Get(client, path + "&limit=1&cursor=" + Uri.EscapeDataString(cursor));
            } while (true);
            Assert.Equal(3, ids.Count);
            var boundCursor = Uri.EscapeDataString(first.GetProperty("nextCursor").GetString()!);
            var endpoint = path.Split('?')[0];
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(endpoint + "?role=CREW&search=changed&cursor=" + boundCursor)).StatusCode);
            foreach (var query in new[] { "limit=0", "limit=101", "cursor=garbage", "role=1", "role=UNKNOWN", "status=999", "limit=abc" })
                Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(endpoint + "?" + query)).StatusCode);
        }
    }

    [Fact]
    public async Task RealWebCookieAndBearerSupportAllReadsAndRevokedSessionsFail()
    {
        var sup = await User(UserRoleCode.Supervisor); var project = await Project();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var cookie = await Login(factory, sup, cookie: true);
        using var bearer = await Login(factory, sup);
        using var anonymous = factory.CreateClient(new() { HandleCookies = false });
        var urls = new[] { "/api/v1/users", $"/api/v1/projects/{project.Id}/members", $"/api/v1/projects/{project.Id}/primary-project-manager", $"/api/v1/users/{sup.Id}" };
        Assert.Null(cookie.DefaultRequestHeaders.Authorization);
        foreach (var url in urls)
        {
            Assert.Equal(HttpStatusCode.OK, (await cookie.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await bearer.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        }
        foreach (var surface in new[] { "members", "primary-project-manager" })
            Assert.Equal(HttpStatusCode.NotFound, (await bearer.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/{surface}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.PatchAsJsonAsync($"/api/v1/users/{sup.Id}", new { })).StatusCode);
        await using (var db = sql.CreateDbContext()) await db.Sessions.Where(x => x.UserId == sup.Id).ExecuteUpdateAsync(x => x.SetProperty(s => s.RevokedAt, DateTimeOffset.UtcNow));
        foreach (var url in urls)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await bearer.GetAsync(url)).StatusCode);
        }
    }

    [Fact]
    public async Task ReadContractsExposeOnlyDeclaredFieldsAndNeverMutateOwnedRows()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager);
        var project = await Project();
        var member = await Member(project.Id, pm.Id, UserRoleCode.ProjectManager, primary: true);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup);
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == pm.Id);
            var membership = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.Id == member.Id);
            var head = await db.Projects.AsNoTracking().SingleAsync(x => x.Id == project.Id);
            return Convert.ToBase64String(user.RowVersion) + ":" + Convert.ToBase64String(membership.RowVersion) + ":" + Convert.ToBase64String(head.RowVersion);
        }
        static void Keys(JsonElement item, params string[] names) =>
            Assert.Equal(names.OrderBy(x => x), item.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
        var before = await Snapshot();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var roster = await Get(client, $"/api/v1/projects/{project.Id}/members");
            Keys(roster, "items", "nextCursor", "asOf");
            var row = Assert.Single(roster.GetProperty("items").EnumerateArray());
            Keys(row, "membershipId", "userId", "displayName", "role", "isPrimary", "status", "validFrom", "validTo", "membershipVersion");
            Assert.Equal(member.Id, row.GetProperty("membershipId").GetGuid());
            Assert.Equal(pm.Id, row.GetProperty("userId").GetGuid());
            Assert.Equal("PM", row.GetProperty("role").GetString());
            var primary = await Get(client, $"/api/v1/projects/{project.Id}/primary-project-manager");
            Keys(primary, "primaryProjectManager");
            var current = primary.GetProperty("primaryProjectManager");
            Keys(current, "membershipId", "userId", "displayName", "validFrom", "validTo", "membershipVersion");
            Assert.Equal(member.Id, current.GetProperty("membershipId").GetGuid());
            Assert.Equal(row.GetProperty("membershipVersion").GetString(), current.GetProperty("membershipVersion").GetString());
            var account = await client.GetAsync($"/api/v1/users/{pm.Id}");
            Assert.Equal(HttpStatusCode.OK, account.StatusCode);
            var detail = await account.Content.ReadFromJsonAsync<JsonElement>();
            Keys(detail, "id", "displayName", "role", "version");
            Assert.Equal(pm.Id, detail.GetProperty("id").GetGuid());
            Assert.Equal(pm.DisplayName, detail.GetProperty("displayName").GetString());
            Assert.Equal("PM", detail.GetProperty("role").GetString());
            Assert.Equal("\"" + detail.GetProperty("version").GetString() + "\"", account.Headers.ETag!.Tag);
            Assert.Equal(before, await Snapshot());
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/users/not-a-guid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/projects/{project.Id}/members?includeHistory=not-a-bool")).StatusCode);
        Assert.Equal(before, await Snapshot());
    }

    [Fact]
    public async Task RosterCursorCannotCrossProjectOrActorAndReadDenialsPreserveMembershipVersion()
    {
        var sup = await User(UserRoleCode.Supervisor); var otherSup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager); var crew = await User(UserRoleCode.RepairCrew);
        var a = await Project(); var b = await Project();
        var pmMember = await Member(a.Id, pm.Id, UserRoleCode.ProjectManager, primary: true);
        await Member(a.Id, crew.Id, UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup); using var other = await Login(factory, otherSup); using var manager = await Login(factory, pm);
        var first = await Get(client, $"/api/v1/projects/{a.Id}/members?limit=1");
        var cursor = Uri.EscapeDataString(first.GetProperty("nextCursor").GetString()!);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/projects/{b.Id}/members?limit=1&cursor={cursor}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await other.GetAsync($"/api/v1/projects/{a.Id}/members?limit=1&cursor={cursor}")).StatusCode);
        foreach (var surface in new[] { "members", "primary-project-manager" })
            Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"/api/v1/projects/{b.Id}/{surface}")).StatusCode);
        await using var db = sql.CreateDbContext();
        Assert.Equal(pmMember.RowVersion, (await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.Id == pmMember.Id)).RowVersion);
        Assert.Empty(await db.ProjectMembers.Where(x => x.ProjectId == b.Id).ToArrayAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApprovedAccountFourFieldWirePreservesContactsAndExactAuthorityErrors(bool cookie)
    {
        var sup = await User(UserRoleCode.Supervisor);
        var target = await User(UserRoleCode.ProjectManager);
        await using (var db = sql.CreateDbContext())
            await db.Users.Where(x => x.Id == target.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.PhoneNumber, "000-test-contact"));
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup, cookie);
        var path = $"/api/v1/users/{target.Id}";
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "displayName", "id", "role", "version" }, body.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
        Assert.Equal(target.Id, body.GetProperty("id").GetGuid());
        Assert.Equal(target.DisplayName, body.GetProperty("displayName").GetString());
        Assert.Equal("PM", body.GetProperty("role").GetString());
        await using (var db = sql.CreateDbContext())
        {
            var stored = await db.Users.AsNoTracking().SingleAsync(x => x.Id == target.Id);
            Assert.Equal(target.Email, stored.Email);
            Assert.Equal("000-test-contact", stored.PhoneNumber);
            Assert.Equal(Convert.ToBase64String(stored.RowVersion), body.GetProperty("version").GetString());
            Assert.Equal('"' + body.GetProperty("version").GetString() + '"', response.Headers.ETag!.Tag);
            Assert.False(await db.ProjectMembers.AnyAsync(x => x.UserId == sup.Id));
        }
        var directory = await Get(client, "/api/v1/users?search=" + target.Email);
        var row = Assert.Single(directory.GetProperty("items").EnumerateArray());
        Assert.Equal(target.Email, row.GetProperty("email").GetString());
        Assert.Equal("000-test-contact", row.GetProperty("phoneNumber").GetString());
        foreach (var role in new[] { UserRoleCode.ProjectManager, UserRoleCode.DroneOperator, UserRoleCode.RepairCrew, UserRoleCode.Reporter })
        {
            using var denied = await Login(factory, await User(role), cookie);
            await Error(await denied.GetAsync(path), HttpStatusCode.Forbidden, "access_forbidden");
        }
        await Error(await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}"), HttpStatusCode.NotFound, "identity_user_not_found");
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);

        static async Task Error(HttpResponseMessage denied, HttpStatusCode status, string code)
        {
            Assert.Equal(status, denied.StatusCode);
            Assert.Equal("application/problem+json", denied.Content.Headers.ContentType!.MediaType);
            var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
            Assert.Equal(code, problem.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("correlationId").GetString()));
            Assert.All(problem.EnumerateObject(), p => Assert.Contains(p.Name, new[] { "type", "title", "status", "detail", "instance", "code", "correlationId" }));
        }
    }

    private Task<ApplicationUser> User(UserRoleCode role, string? name = null) => sql.CreateUserAsync(name ?? "STAFF-" + Guid.NewGuid().ToString("N"), "Current1!", role);
    private async Task<Project> Project()
    {
        var row = RoadGuardSystem.BusinessObjects.Projects.Project.Create(Guid.NewGuid(), "STAFF-P-" + Guid.NewGuid().ToString("N"), "Staff read fixture", null, null, null, null, DateTimeOffset.UtcNow);
        await using var db = sql.CreateDbContext(); db.Add(row); await db.SaveChangesAsync(); return row;
    }
    private async Task<ProjectMember> Member(Guid project, Guid user, UserRoleCode role, bool primary = false, DateOnly? from = null, DateOnly? until = null, ProjectMemberStatus status = ProjectMemberStatus.Active)
    {
        var row = new ProjectMember { Id = Guid.NewGuid(), ProjectId = project, UserId = user, RoleCode = role, IsPrimary = primary, Status = status, ValidFrom = from ?? Today.AddDays(-2), ValidTo = until };
        await using var db = sql.CreateDbContext(); db.Add(row); await db.SaveChangesAsync(); return row;
    }
    private static async Task<JsonElement> Get(HttpClient client, string url)
    {
        var response = await client.GetAsync(url); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static async Task<HttpClient> Login(AuthenticationWebApplicationFactory factory, ApplicationUser user, bool cookie = false)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = cookie });
        if (cookie)
        {
            var csrf = await Get(client, "/api/v1/auth/web/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("requestToken").GetString());
        }
        var login = await client.PostAsJsonAsync(cookie ? "/api/v1/auth/web/login" : "/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        if (cookie) client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        else client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }
}
