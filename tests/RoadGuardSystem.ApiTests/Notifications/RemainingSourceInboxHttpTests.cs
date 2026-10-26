using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.ApiTests.Notifications;

public sealed class RemainingSourceInboxHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("Project")]
    [InlineData("Defect")]
    [InlineData("Case")]
    [InlineData("IncidentCase")]
    [InlineData("RoadSectionVersion")]
    [InlineData("SurveyRequest")]
    [InlineData("Survey")]
    [InlineData("FieldTask")]
    [InlineData("FieldInspectionTask")]
    [InlineData("DeadlineClock")]
    [InlineData("ReviewObligation")]
    [InlineData("ReviewDigest")]
    [InlineData("ReviewDigest:PM")]
    [InlineData("TemporarySafetyMeasure")]
    [InlineData("Project:Supervisor")]
    [InlineData("Project:DroneOperator")]
    [InlineData("Defect:Supervisor")]
    [InlineData("Defect:DroneOperator")]
    [InlineData("Case:Supervisor")]
    [InlineData("Case:DroneOperator")]
    [InlineData("IncidentCase:Supervisor")]
    [InlineData("IncidentCase:DroneOperator")]
    [InlineData("RoadSectionVersion:Supervisor")]
    [InlineData("RoadSectionVersion:DroneOperator")]
    [InlineData("SurveyRequest:Supervisor")]
    [InlineData("SurveyRequest:DroneOperator")]
    [InlineData("Survey:Supervisor")]
    [InlineData("Survey:DroneOperator")]
    [InlineData("FieldTask:Supervisor")]
    [InlineData("FieldTask:DroneOperator")]
    [InlineData("FieldInspectionTask:Supervisor")]
    [InlineData("FieldInspectionTask:DroneOperator")]
    [InlineData("TemporarySafetyMeasure:Crew")]
    [InlineData("DeadlineClock:Crew")]
    [InlineData("FieldTask:Crew")]
    [InlineData("FieldInspectionTask:Crew")]
    [InlineData("DeadlineClock:ScopePM")]
    [InlineData("DeadlineClock:ScopeDrone")]
    [InlineData("ReviewObligation:ScopePM")]
    [InlineData("ReviewObligation:ScopeDrone")]
    [InlineData("TemporarySafetyMeasure:ScopePM")]
    [InlineData("TemporarySafetyMeasure:ScopeDrone")]
    public async Task SourceSpecificInboxRechecksCurrentAuthorityBeforeReadAndReplay(string kind)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        await using var db = sql.CreateDbContext();
        var actual = await GenuineProtectedSource.Seed(db, sql);
        var clock = new TestClock();
        var variant = kind.Split(':'); kind = variant[0];
        var recipient = variant.Length > 1 && variant[1] == "Crew" ? actual.Crew : actual.Pm;
        if (variant.Length > 1 && variant[1] == "Supervisor") recipient = actual.Supervisor;
        if (variant.Length > 1 && variant[1] is "DroneOperator" or "ScopeDrone")
        {
            var drone = await sql.CreateUserAsync("legacy-drone-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
            recipient = drone.Id;
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = actual.Project,
                UserId = recipient,
                RoleCode = UserRoleCode.DroneOperator,
                IsPrimary = false,
                ValidFrom = new(2000, 1, 1),
                Status = ProjectMemberStatus.Active
            });
            await db.SaveChangesAsync();
        }
        var scopedRecipient = recipient;
        Guid sourceId;
        Notification notification;
        var genuineProducer = kind is "DeadlineClock" or "ReviewDigest" or "TemporarySafetyMeasure";
        if (kind == "ReviewDigest" && variant.Length > 1 && variant[1] == "PM")
        {
            var version = Convert.ToBase64String(await db.Defects.Where(d => d.Id == actual.Defect).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync());
            var repo = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), clock);
            var created = await repo.ExecuteAsync(new(actual.Project, null, "create", new FieldTaskCreateInputFact(actual.Defect, version, null, "REPORTER", actual.Route, actual.Set, null, null, "PRE_MEASUREMENT", 1, "{}", "digest duty", actual.Crew, clock.Now.AddDays(1)), Guid.NewGuid().ToString(), null, new(actual.Pm, UserRoleCode.ProjectManager, actual.Pm, "DIRECT", true)), _ => Task.FromResult(true), default);
            Assert.Equal(201, created.Status); var taskId = (await db.FieldInspectionTasks.SingleAsync(t => t.ProjectId == actual.Project)).Id;
            async Task Run(string action, object input)
            {
                db.ChangeTracker.Clear(); var taskVersion = Convert.ToBase64String(await db.FieldInspectionTasks.Where(t => t.Id == taskId).Select(t => t.RowVersion).SingleAsync());
                var result = await repo.ExecuteAsync(new(actual.Project, taskId, action, input, Guid.NewGuid().ToString(), taskVersion, new(actual.Crew, UserRoleCode.RepairCrew, actual.Crew, "DIRECT", true)), _ => Task.FromResult(true), default);
                Assert.InRange(result.Status, 200, 201);
            }
            await Run("accept", new FieldTaskActionInputFact("actual assigned Crew"));
            await Run("start", new FieldStartInputFact(Guid.NewGuid(), clock.Now));
            var start = await db.FieldTaskStartOrigins.SingleAsync(x => x.TaskId == taskId);
            await Run("submit", new FieldSubmissionInputFact(Guid.NewGuid(), start.Id, null, [], [], null, "MEASUREMENT", null, null));
            var duty = await db.Set<DeadlineClock>().SingleAsync(c => c.TargetId == taskId && c.Kind == DeadlineClockKind.ProjectManagerReview);
            var dispatcher = new H6NotificationDispatchRepository(db, clock);
            clock.Now = NotificationCalendarPolicy.NextWeeklyReview(duty.OriginAt).AddMinutes(-1);
            await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default); clock.Now = clock.Now.AddDays(7).AddMinutes(2);
            await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default);
            var digest = await db.Set<WeeklyReviewDigest>().SingleAsync(d => d.ProjectId == actual.Project); sourceId = digest.Id;
            Assert.Equal(actual.Pm, digest.RecipientId); Assert.Equal(UserRoleCode.ProjectManager, digest.RecipientRole);
            var message = await db.OutboxMessages.SingleAsync(m => m.Id == digest.Id); var fence = Guid.NewGuid();
            message.AcquireLease("h6:" + fence.ToString("N"), clock.Now, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
            var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
            var dispatched = await dispatcher.DispatchAsync(claim, H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
            Assert.Equal("COMMITTED", dispatched.Status); Assert.Equal(1, dispatched.Delivered);
            notification = await db.Notifications.SingleAsync(n => n.SourceEntityId == sourceId && n.RecipientUserId == actual.Pm);
        }
        else if (kind is "DeadlineClock" or "ReviewObligation" or "ReviewDigest" or "TemporarySafetyMeasure")
        {
            var version = Convert.ToBase64String(await db.Defects.Where(d => d.Id == actual.Defect).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync());
            var repair = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), clock);
            var created = await repair.CreatePackageAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project,
                new(actual.Defect, version, [new("FORMAL_REPAIR", true, new(actual.Road, actual.Route, actual.Set, null, null, 1, 2, 0, 1), "formal"),
                new("TEMPORARY_SAFETY", true, new(actual.Road, actual.Route, actual.Set, null, null, 1, 2, 0, 1), "safety")], "actual package"), Guid.NewGuid().ToString(), version), default);
            Assert.Equal(201, created.Status);
            var packageView = Assert.IsType<RepairPackageFact>(created.Value);
            var package = await db.Set<RepairPackage>().Include(p => p.Obligations).SingleAsync(p => p.Id == packageView.Id);
            var formal = package.Obligations.Single(o => o.Kind == RepairObligationKind.FormalRepair);
            var proposed = await repair.ProposeItemAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project, package.Id,
                new(formal.Id, "NORMAL", "approved workflow fixture", "checklist-v1", "actual proposal"), Guid.NewGuid().ToString(), packageView.Version), default);
            Assert.Equal(201, proposed.Status); var item = Assert.IsType<RepairItemFact>(proposed.Value);
            var duty = await db.Set<DeadlineClock>().SingleAsync(c => c.TargetId == item.Id && c.Kind == DeadlineClockKind.SupervisorInitialApproval);
            var dispatcher = new H6NotificationDispatchRepository(db, clock);
            Guid messageId;
            if (kind == "TemporarySafetyMeasure" || kind == "DeadlineClock" && variant.Length > 1 && variant[1] == "Crew")
            {
                var approved = await repair.ApproveItemAsync(new(actual.Supervisor, UserRoleCode.Supervisor, actual.Project, package.Id, item.Id, new("actual approval"), Guid.NewGuid().ToString(), item.Version), default);
                Assert.Equal(201, approved.Status); var approvedItem = Assert.IsType<RepairItemFact>(approved.Value);
                version = Convert.ToBase64String(await db.Defects.Where(d => d.Id == actual.Defect).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync());
                var assigned = await repair.AssignItemAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project, package.Id, item.Id,
                    new(new(actual.Defect, version, null, "REPORTER", actual.Route, actual.Set, null, null, "POST_REPAIR", 1, "{}", null, actual.Crew, clock.Now.AddDays(1)), null, "actual assignment"), Guid.NewGuid().ToString(), approvedItem.Version), default);
                Assert.Equal(201, assigned.Status);
                var current = await db.Set<RepairItem>().SingleAsync(i => i.Id == item.Id);
                var safety = await new RepairSafetyRepository(db, new IdempotencyOperationService(db), clock).CreateAsync(
                    new(actual.Pm, UserRoleCode.ProjectManager, actual.Project, package.Id, item.Id, formal.Id,
                        package.Obligations.Single(o => o.Kind == RepairObligationKind.TemporarySafety).Id, actual.Crew,
                        "each shift", "replace if displaced", "remove after formal repair", "actual barrier", Guid.NewGuid().ToString(),
                        Convert.ToBase64String(db.Entry(current).Property<byte[]>("RowVersion").CurrentValue!)), default);
                Assert.Equal(201, safety.Status); var measure = Assert.IsType<SafetyFact>(safety.Value); sourceId = measure.Id;
                recipient = variant.Length > 1 && variant[1] == "Crew" ? actual.Crew : actual.Supervisor;
                messageId = (await db.Set<RepairSafetyActionSource>().SingleAsync(a => a.MeasureId == sourceId && a.Kind == "ASSIGNED")).Id;
                if (kind == "DeadlineClock")
                {
                    var installed = await new RepairSafetyRepository(db, new IdempotencyOperationService(db), clock).InstallAsync(
                        new(actual.Crew, UserRoleCode.RepairCrew, actual.Project, package.Id, item.Id, measure.Id,
                            clock.Now.AddMinutes(5), "actual installed barrier", Guid.NewGuid().ToString(), measure.Version), default);
                    Assert.Equal(201, installed.Status);
                    duty = await db.Set<DeadlineClock>().SingleAsync(c => c.TargetId == measure.Id && c.Kind == DeadlineClockKind.FirstSafetyCheck);
                    sourceId = duty.Id; clock.Now = duty.OriginalDueAt.AddSeconds(1); await dispatcher.ObserveClocksAsync(default);
                    messageId = (await db.Set<DeadlineBreach>().SingleAsync(b => b.ClockId == duty.Id)).Id;
                }
                else if (recipient == actual.Crew) genuineProducer = false; // ASSIGNED producer targets Supervisor; Crew legacy inbox tests the responsible-actor reader only.
            }
            else if (kind == "ReviewDigest")
            {
                clock.Now = NotificationCalendarPolicy.NextWeeklyReview(duty.OriginAt).AddMinutes(-1);
                await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default);
                clock.Now = clock.Now.AddDays(7).AddMinutes(2);
                await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default);
                var digest = await db.Set<WeeklyReviewDigest>().SingleAsync(d => d.ProjectId == actual.Project);
                sourceId = digest.Id; messageId = digest.Id; recipient = actual.Supervisor;
            }
            else
            {
                sourceId = duty.Id; recipient = actual.Supervisor;
                clock.Now = duty.OriginalDueAt.AddMinutes(1);
                await dispatcher.ObserveClocksAsync(default);
                messageId = (await db.Set<DeadlineBreach>().SingleAsync(b => b.ClockId == duty.Id)).Id;
            }
            if (genuineProducer)
            {
                var message = await db.OutboxMessages.SingleAsync(m => m.Id == messageId); var fence = Guid.NewGuid();
                message.AcquireLease("h6:" + fence.ToString("N"), clock.Now, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
                var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
                var dispatched = await dispatcher.DispatchAsync(claim, H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
                Assert.Equal("COMMITTED", dispatched.Status);
                var expectedRecipients = kind == "DeadlineClock" && recipient == actual.Crew
                    ? new[] { actual.Crew, actual.Supervisor } : new[] { recipient };
                Assert.Equal(expectedRecipients.Length, dispatched.Delivered);
                var deliveredRecipients = await db.Notifications.Where(n => n.SourceEntityId == sourceId).Select(n => n.RecipientUserId).ToArrayAsync();
                Assert.Equal(expectedRecipients.OrderBy(x => x), deliveredRecipients.OrderBy(x => x));
                notification = await db.Notifications.SingleAsync(n => n.SourceEntityId == sourceId && n.RecipientUserId == recipient);
            }
            else
            {
                // Legacy ReviewObligation read branch: real clock, persisted inbox; no claim of a modern producer for this alias.
                notification = Notification.Create(Guid.NewGuid(), recipient, kind, sourceId, "legacy", "legacy review", "synthetic legacy review", clock.Now);
                db.Add(notification); await db.SaveChangesAsync();
            }
        }
        else
        {
            if (kind is "Survey" or "SurveyRequest")
            {
                var request = SurveyRequest.Create(Guid.NewGuid(), actual.Project, actual.Road, null, actual.Pm, SurveyType.Periodic, SurveyRequestStatus.Accepted, clock.Now); request.SetBandScope();
                var survey = Survey.Create(Guid.NewGuid(), request.Id, actual.Project, actual.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
                db.AddRange(request, survey); await db.SaveChangesAsync(); sourceId = kind == "Survey" ? survey.Id : request.Id;
            }
            else if (kind is "FieldTask" or "FieldInspectionTask")
            {
                var version = Convert.ToBase64String(await db.Defects.Where(d => d.Id == actual.Defect).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync());
                var field = await new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), clock).ExecuteAsync(
                    new(actual.Project, null, "create", new FieldTaskCreateInputFact(actual.Defect, version, null, "REPORTER", actual.Route, actual.Set, null, null, "PRE_MEASUREMENT", 1, "{}", "actual field", actual.Crew, clock.Now.AddDays(1)), Guid.NewGuid().ToString(), null, new(actual.Pm, UserRoleCode.ProjectManager, actual.Pm, "DIRECT", true)), _ => Task.FromResult(true), default);
                Assert.Equal(201, field.Status); sourceId = (await db.FieldInspectionTasks.SingleAsync(t => t.ProjectId == actual.Project)).Id;
            }
            else sourceId = kind switch
            {
                "Project" => actual.Project,
                "Defect" => actual.Defect,
                "RoadSectionVersion" => actual.Route,
                _ => (await db.IncidentCases.SingleAsync(c => c.ProjectId == actual.Project)).Id
            };
            // Supported legacy read-only shape, not a fabricated producer occurrence.
            notification = Notification.Create(Guid.NewGuid(), recipient, kind, sourceId, "legacy", "legacy fixture", "synthetic legacy body", clock.Now);
            db.Add(notification); await db.SaveChangesAsync();
        }
        if (variant.Length > 1 && variant[1] is "ScopePM" or "ScopeDrone")
        {
            // Existing reader scope accepts these current project roles. This is
            // a persisted inbox fixture, not a new producer/recipient policy.
            recipient = scopedRecipient;
            notification = Notification.Create(Guid.NewGuid(), recipient, kind, sourceId,
                "legacy-scope", "reader scope", "synthetic supported scope", notification.OccurredAtUtc);
            db.Add(notification); await db.SaveChangesAsync();
        }
        // Additional persisted inbox occurrence exercises paging for this exact valid source; not producer proof.
        var older = Notification.Create(Guid.NewGuid(), recipient, kind, sourceId, "legacy-page", "older fixture", "bounded paging projection", notification.OccurredAtUtc.AddMinutes(-1));
        db.Add(older); await db.SaveChangesAsync();
        // Production JWT validation uses wall time. Advance only the producer clock, then return to real time before login.
        clock.Now = DateTimeOffset.UtcNow;
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: s => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<TimeProvider>(s, clock));
        using var client = factory.CreateClient(); using var wrong = factory.CreateClient();
        await Login(client, (await db.Users.SingleAsync(u => u.Id == recipient)).Email!);
        var outsider = await sql.CreateUserAsync("wrong-family-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager); await Login(wrong, outsider.Email!);
        var path = $"/api/v1/notifications/{notification.Id}";
        var get = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var dto = await get.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(6, dto.EnumerateObject().Count());
        Assert.Equal(sourceId, dto.GetProperty("resourceId").GetGuid()); Assert.Equal(notification.Body, dto.GetProperty("message").GetString());
        var version0 = dto.GetProperty("version").GetString()!; Assert.Equal("\"" + version0 + "\"", get.Headers.ETag!.Tag);
        var page = await (await client.GetAsync("/api/v1/notifications?limit=1")).Content.ReadFromJsonAsync<JsonElement>();
        var first = Assert.Single(page.GetProperty("items").EnumerateArray());
        var foundTarget = first.GetProperty("id").GetGuid() == notification.Id;
        var foundOlder = first.GetProperty("id").GetGuid() == older.Id;
        var previous = first.GetProperty("occurredAt").GetDateTimeOffset();
        var cursor = page.GetProperty("nextCursor").GetString();
        for (var i = 0; i < 20 && cursor is not null && !(foundTarget && foundOlder); i++)
        {
            var next = await (await client.GetAsync("/api/v1/notifications?limit=1&cursor=" + Uri.EscapeDataString(cursor))).Content.ReadFromJsonAsync<JsonElement>();
            var item = Assert.Single(next.GetProperty("items").EnumerateArray());
            var at = item.GetProperty("occurredAt").GetDateTimeOffset(); Assert.True(at <= previous); previous = at;
            foundTarget |= item.GetProperty("id").GetGuid() == notification.Id;
            foundOlder |= item.GetProperty("id").GetGuid() == older.Id;
            cursor = next.GetProperty("nextCursor").GetString();
        }
        Assert.True(foundTarget); Assert.True(foundOlder);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/notifications?limit=0")).StatusCode);
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Read(anonymous, notification.Id, "anonymous", version0)).StatusCode);
        Assert.DoesNotContain(notification.Id.ToString(), await wrong.GetStringAsync("/api/v1/notifications"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, (await wrong.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Read(wrong, notification.Id, Guid.NewGuid().ToString(), version0)).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await client.PostAsync(path + "/read", null)).StatusCode);
        var keys = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
        var racers = await Task.WhenAll(keys.Select(k => Read(client, notification.Id, k, version0)));
        Assert.Single(racers.Where(r => r.StatusCode == HttpStatusCode.OK)); Assert.Single(racers.Where(r => r.StatusCode == HttpStatusCode.PreconditionFailed));
        var winner = Array.FindIndex(racers, r => r.StatusCode == HttpStatusCode.OK); var key = keys[winner]; var mark = racers[winner];
        Assert.Equal(await mark.Content.ReadAsStringAsync(), await (await Read(client, notification.Id, key, version0)).Content.ReadAsStringAsync());
        // Suspension must invalidate both new reads and prior command replay, for each supported source family.
        await db.Users.Where(u => u.Id == recipient).ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/notifications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Read(client, notification.Id, key, version0)).StatusCode);
        await db.Users.Where(u => u.Id == recipient).ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, UserStatus.Active));
        await Login(client, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == recipient)).Email!);
        var marked = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == notification.Id);
        var receipts = await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient);
        var audits = await db.Set<H6NotificationAuditRow>().CountAsync(a => a.NotificationId == notification.Id); Assert.True(audits > 0);
        if (kind == "ReviewDigest" && recipient == actual.Pm)
        {
            // A captured digest is not reassigned by changing the current primary PM.
            // The captured actor retains existing membership-based read authority until revocation.
            var replacement = await sql.CreateUserAsync("digest-new-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
            await db.ProjectMembers.Where(m => m.ProjectId == actual.Project && m.UserId == recipient).ExecuteUpdateAsync(m => m.SetProperty(x => x.IsPrimary, false));
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = actual.Project,
                UserId = replacement.Id,
                RoleCode = UserRoleCode.ProjectManager,
                IsPrimary = true,
                ValidFrom = new(2000, 1, 1),
                Status = ProjectMemberStatus.Active
            }); await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
            using var newPm = factory.CreateClient(); await Login(newPm, replacement.Email!);
            Assert.Equal(HttpStatusCode.NotFound, (await newPm.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Read(newPm, notification.Id, Guid.NewGuid().ToString(), version0)).StatusCode);
            Assert.DoesNotContain(notification.Id.ToString(), await newPm.GetStringAsync("/api/v1/notifications"), StringComparison.OrdinalIgnoreCase);
        }
        if (recipient == actual.Supervisor)
        {
            // Supervisor reads are global in current implementation, so membership loss alone is not a denial rule.
            await db.Users.Where(u => u.Id == recipient).ExecuteUpdateAsync(u => u.SetProperty(x => x.RoleCode, UserRoleCode.Reporter));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
            await Login(client, (await db.Users.SingleAsync(u => u.Id == recipient)).Email!);
        }
        else
        {
            await db.ProjectMembers.Where(m => m.ProjectId == actual.Project && m.UserId == recipient).ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
            var projectB = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "isolated B", null, null, null, null, clock.Now);
            db.Add(projectB);
            var currentRole = await db.Users.Where(u => u.Id == recipient).Select(u => u.RoleCode).SingleAsync();
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectB.Id,
                UserId = recipient,
                RoleCode = currentRole,
                IsPrimary = currentRole == UserRoleCode.ProjectManager,
                ValidFrom = new(2000, 1, 1),
                Status = ProjectMemberStatus.Active
            });
            await db.SaveChangesAsync();
            // Membership in B cannot recover source A after A membership ends; no role/assignment transfer inferred.
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.DoesNotContain(notification.Id.ToString(), await client.GetStringAsync("/api/v1/notifications"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, (await Read(client, notification.Id, key, version0)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Read(client, notification.Id, Guid.NewGuid().ToString(), version0)).StatusCode);
        var final = await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == notification.Id);
        Assert.Equal(marked.ReadAt, final.ReadAt); Assert.Equal(marked.RowVersion, final.RowVersion);
        Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == recipient));
        Assert.Equal(audits, await db.Set<H6NotificationAuditRow>().CountAsync(a => a.NotificationId == notification.Id));
    }
    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Read(HttpClient client, Guid id, string key, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/notifications/{id}/read"); request.Headers.Add("Idempotency-Key", key); request.Headers.Add("If-Match", "\"" + version + "\""); return client.SendAsync(request);
    }
    private sealed class TestClock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
}
