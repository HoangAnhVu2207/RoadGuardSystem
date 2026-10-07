using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Implementations.Retention;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LD06LifecycleHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task ActualDeclarationConfirmationEnforcesRoleVersionConflictAndRevokedReplay()
    {
        var pm = await fixture.CreateUserAsync("ld06-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await fixture.CreateUserAsync("ld06-supervisor-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "TEST_ONLY lifecycle HTTP", null, null, null, null, now);
        var membership = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        db.AddRange(project, membership, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)));
        await db.SaveChangesAsync();
        // Synthetic verified evidence exercises production lifecycle consumption; no provider verification claim.
        var file = StoredFile.Create(Guid.NewGuid(), "TEST_ONLY/ld06-http/" + Guid.NewGuid(), "construction.jpg", "image/jpeg", 4, new string('d', 64), pm.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, pm.Id, file.StorageUri, "CONSTRUCTION", "image/jpeg", 4, file.Checksum, 8388608, now.AddHours(24));
        upload.StartUploading("TEST_ONLY", now);
        db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, project.Id, pm.Id, "CONSTRUCTION", now), upload); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var pmClient = factory.CreateClient(); using var supClient = factory.CreateClient();
        await Login(pmClient, pm.Email!); await Login(supClient, supervisor.Email!);
        var path = $"/api/v1/projects/{project.Id}/lifecycle";
        var initial = await pmClient.GetAsync(path); Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var declarationInput = new LD06LifecycleInputDto("TEST_ONLY actual HTTP declaration", [file.Id]);
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await pmClient.PostAsJsonAsync(path + "/construction-declarations", declarationInput)).StatusCode);
        var key = Guid.NewGuid().ToString(); Headers(pmClient, key, initial.Headers.ETag!.ToString());
        var declared = await pmClient.PostAsJsonAsync(path + "/construction-declarations", declarationInput);
        Assert.Equal(HttpStatusCode.Created, declared.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await pmClient.PostAsJsonAsync(path + "/construction-declarations", declarationInput)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await pmClient.PostAsJsonAsync(path + "/construction-declarations", declarationInput with { Reason = "changed" })).StatusCode);
        Headers(pmClient, Guid.NewGuid().ToString(), initial.Headers.ETag.ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await pmClient.PostAsJsonAsync(path + "/construction-declarations", declarationInput)).StatusCode);
        var view = await declared.Content.ReadFromJsonAsync<ProjectLifecycleViewDto>(); Assert.NotNull(view);
        var action = Assert.Single(view.Actions!);
        var confirmation = new LD06LifecycleInputDto("TEST_ONLY exact supervisor confirmation", [], action.Id);
        Headers(pmClient, Guid.NewGuid().ToString(), declared.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await pmClient.PostAsJsonAsync(path + "/construction-confirmations", confirmation)).StatusCode);
        var confirmationKey = Guid.NewGuid().ToString(); Headers(supClient, confirmationKey, declared.Headers.ETag.ToString());
        var confirmed = await supClient.PostAsJsonAsync(path + "/construction-confirmations", confirmation);
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        Assert.Equal("CONFIRMED", (await confirmed.Content.ReadFromJsonAsync<ProjectLifecycleViewDto>())!.ConstructionCompletion);
        await db.ProjectMembers.Where(row => row.Id == membership.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await supClient.PostAsJsonAsync(path + "/construction-confirmations", confirmation)).StatusCode);
        Assert.Equal(2, await db.Set<LD06LifecycleAction>().CountAsync(row => row.ProjectId == project.Id));
        var retention = new Huy02InspectionRetentionContributor(db);
        Assert.Contains(file.Id, await retention.KnownProjectFilesAsync(project.Id, default));
        var inventory = await retention.ReadAsync(file.Id, default);
        Assert.True(inventory.Complete);
        Assert.Contains(inventory.References, row => row.Kind == "LIFECYCLE_ACTION_EVIDENCE" && row.Id == action.Id && row.ProjectId == project.Id);
    }

    private static void Headers(HttpClient client, string key, string version)
    {
        client.DefaultRequestHeaders.Remove("Idempotency-Key"); client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("Idempotency-Key", key); client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", version);
    }
    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
}
