using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Surveys;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Surveys;
using Xunit;

namespace RoadGuardSystem.ApiTests.Notifications;

/// <summary>
/// RF-10-09-C01: Characterization of notification inbox HTTP authorization, projection,
/// and outbox→inbox producer linkage. Focuses on recipient scope, cursor pagination,
/// mark-read idempotency, and outbox consumer replay. Does NOT verify dispatcher/scheduler
/// or real delivery timing.
/// </summary>
[Trait("TaskId", "RF-10-09-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Rf1009NotificationInboxCharacterizationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public Rf1009NotificationInboxCharacterizationTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact(DisplayName = "RF-10-09-C01: inbox GET filters by recipient and projects correct fields")]
    public async Task InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields()
    {
        // Arrange: create two users and notifications for each
        var recipient = await _sql.CreateUserAsync($"rf1009_recipient_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var outsider = await _sql.CreateUserAsync($"rf1009_outsider_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);

        var recipientNotification = Notification.Create(
            Guid.NewGuid(),
            recipient.Id,
            "SurveyRequest",
            Guid.NewGuid(),
            "survey_request.created",
            "Survey request created",
            "A new survey request was created for your project.",
            DateTimeOffset.UtcNow.AddMinutes(-10));

        var outsiderNotification = Notification.Create(
            Guid.NewGuid(),
            outsider.Id,
            "ProcessingJob",
            Guid.NewGuid(),
            "processing_job.completed",
            "Processing completed",
            "AI processing job has completed.",
            DateTimeOffset.UtcNow.AddMinutes(-5));

        byte[] recipientVersion;
        await using (var setup = _sql.CreateDbContext())
        {
            setup.Notifications.AddRange(recipientNotification, outsiderNotification);
            await setup.SaveChangesAsync();
            recipientVersion = recipientNotification.RowVersion;
        }

        // Fresh baseline: capture state before GET
        NotificationSnapshot baselineRecipient;
        NotificationSnapshot baselineOutsider;
        await using (var baseline = _sql.CreateDbContext())
        {
            baselineRecipient = await CaptureNotificationAsync(baseline, recipientNotification.Id);
            baselineOutsider = await CaptureNotificationAsync(baseline, outsiderNotification.Id);
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var recipientClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var outsiderClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AuthenticateAsync(recipientClient, recipient.UserName!);
        await AuthenticateAsync(outsiderClient, outsider.UserName!);

        // Act: GET own notification and attempt GET of other's notification
        var recipientGet = await recipientClient.GetAsync($"/api/v1/notifications/{recipientNotification.Id}");
        var recipientCrossGet = await recipientClient.GetAsync($"/api/v1/notifications/{outsiderNotification.Id}");
        var outsiderCrossGet = await outsiderClient.GetAsync($"/api/v1/notifications/{recipientNotification.Id}");

        // Assert HTTP responses
        recipientGet.StatusCode.Should().Be(HttpStatusCode.OK);
        recipientCrossGet.StatusCode.Should().Be(HttpStatusCode.NotFound, "recipient cannot see outsider's notification");
        outsiderCrossGet.StatusCode.Should().Be(HttpStatusCode.NotFound, "outsider cannot see recipient's notification");

        var body = await recipientGet.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().Be(recipientNotification.Id);
        body.GetProperty("message").GetString().Should().Be("A new survey request was created for your project.");
        body.GetProperty("resourceId").GetGuid().Should().Be(recipientNotification.SourceEntityId);
        body.GetProperty("read").GetBoolean().Should().BeFalse();
        body.GetProperty("occurredAt").GetDateTimeOffset().Should().BeCloseTo(recipientNotification.OccurredAtUtc, TimeSpan.FromSeconds(1));

        var versionFromBody = body.GetProperty("version").GetString();
        versionFromBody.Should().NotBeNullOrWhiteSpace();
        var etag = recipientGet.Headers.ETag!.Tag;
        etag.Should().Be($"\"{versionFromBody}\"");

        // Assert SQL immutability: no changes to either notification after GET operations
        await using var verify = _sql.CreateDbContext();
        var afterRecipient = await CaptureNotificationAsync(verify, recipientNotification.Id);
        var afterOutsider = await CaptureNotificationAsync(verify, outsiderNotification.Id);

        afterRecipient.Id.Should().Be(baselineRecipient.Id);
        afterRecipient.RecipientUserId.Should().Be(baselineRecipient.RecipientUserId);
        afterRecipient.SourceEntityType.Should().Be(baselineRecipient.SourceEntityType);
        afterRecipient.SourceEntityId.Should().Be(baselineRecipient.SourceEntityId);
        afterRecipient.EventType.Should().Be(baselineRecipient.EventType);
        afterRecipient.Title.Should().Be(baselineRecipient.Title);
        afterRecipient.Body.Should().Be(baselineRecipient.Body);
        afterRecipient.OccurredAtUtc.Should().Be(baselineRecipient.OccurredAtUtc);
        afterRecipient.ReadAt.Should().BeNull("GET should not mutate ReadAt");
        afterRecipient.RowVersion.Should().Equal(baselineRecipient.RowVersion);

        afterOutsider.Id.Should().Be(baselineOutsider.Id);
        afterOutsider.RecipientUserId.Should().Be(baselineOutsider.RecipientUserId);
        afterOutsider.ReadAt.Should().BeNull();
        afterOutsider.RowVersion.Should().Equal(baselineOutsider.RowVersion);
    }

    [Fact(DisplayName = "RF-10-09-C01: inbox List filters by recipient and paginates with cursor")]
    public async Task InboxList_RecipientScopeAndCursor_FiltersAndPaginatesCorrectly()
    {
        // Arrange: create recipient with multiple notifications and outsider with own notification
        var recipient = await _sql.CreateUserAsync($"rf1009_list_recipient_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var outsider = await _sql.CreateUserAsync($"rf1009_list_outsider_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);

        var n1 = Notification.Create(Guid.NewGuid(), recipient.Id, "Task", Guid.NewGuid(), "task.assigned", "Task 1", "Body 1", DateTimeOffset.UtcNow.AddMinutes(-30));
        var n2 = Notification.Create(Guid.NewGuid(), recipient.Id, "Task", Guid.NewGuid(), "task.updated", "Task 2", "Body 2", DateTimeOffset.UtcNow.AddMinutes(-20));
        var n3 = Notification.Create(Guid.NewGuid(), recipient.Id, "Task", Guid.NewGuid(), "task.completed", "Task 3", "Body 3", DateTimeOffset.UtcNow.AddMinutes(-10));
        var outsiderN = Notification.Create(Guid.NewGuid(), outsider.Id, "Task", Guid.NewGuid(), "task.assigned", "Outsider task", "Outsider body", DateTimeOffset.UtcNow.AddMinutes(-15));

        await using (var setup = _sql.CreateDbContext())
        {
            setup.Notifications.AddRange(n1, n2, n3, outsiderN);
            await setup.SaveChangesAsync();
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AuthenticateAsync(client, recipient.UserName!);

        // Act: List without cursor, then with cursor
        var firstPage = await client.GetAsync("/api/v1/notifications?limit=2");
        firstPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await firstPage.Content.ReadFromJsonAsync<JsonElement>();
        var firstItems = firstBody.GetProperty("items").EnumerateArray().ToList();
        firstItems.Should().HaveCount(2);

        // Most recent first (DESC ordering)
        firstItems[0].GetProperty("id").GetGuid().Should().Be(n3.Id);
        firstItems[1].GetProperty("id").GetGuid().Should().Be(n2.Id);

        var cursor = firstBody.GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrWhiteSpace();

        var secondPage = await client.GetAsync($"/api/v1/notifications?cursor={Uri.EscapeDataString(cursor!)}&limit=2");
        secondPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await secondPage.Content.ReadFromJsonAsync<JsonElement>();
        var secondItems = secondBody.GetProperty("items").EnumerateArray().ToList();
        secondItems.Should().HaveCount(1, "only one notification remains");
        secondItems[0].GetProperty("id").GetGuid().Should().Be(n1.Id);

        // Verify outsider notification NOT included in recipient's list
        var allIds = firstItems.Concat(secondItems).Select(item => item.GetProperty("id").GetGuid()).ToList();
        allIds.Should().NotContain(outsiderN.Id, "recipient should not see outsider's notification");
    }

    [Fact(DisplayName = "RF-10-09-C01: mark read enforces recipient scope, version check, and idempotency")]
    public async Task MarkRead_RecipientVersionIdempotency_EnforcesConstraintsAndReplays()
    {
        // Arrange
        var recipient = await _sql.CreateUserAsync($"rf1009_markread_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var outsider = await _sql.CreateUserAsync($"rf1009_markread_outsider_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var notification = Notification.Create(
            Guid.NewGuid(),
            recipient.Id,
            "Defect",
            Guid.NewGuid(),
            "defect.detected",
            "Defect detected",
            "A new defect was detected in your project.",
            DateTimeOffset.UtcNow.AddMinutes(-5));

        byte[] originalVersion;
        await using (var setup = _sql.CreateDbContext())
        {
            setup.Notifications.Add(notification);
            await setup.SaveChangesAsync();
            originalVersion = notification.RowVersion;
        }

        // Baseline before mark-read
        NotificationSnapshot baseline;
        await using (var baselineCtx = _sql.CreateDbContext())
        {
            baseline = await CaptureNotificationAsync(baselineCtx, notification.Id);
            baseline.ReadAt.Should().BeNull("notification starts unread");
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var recipientClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var outsiderClient = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AuthenticateAsync(recipientClient, recipient.UserName!);
        await AuthenticateAsync(outsiderClient, outsider.UserName!);

        var version = Convert.ToBase64String(originalVersion);
        var staleVersion = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        // Act & Assert: wrong recipient returns 404
        var outsiderAttempt = await MarkReadAsync(outsiderClient, notification.Id, "outsider-key", version);
        outsiderAttempt.StatusCode.Should().Be(HttpStatusCode.NotFound, "outsider cannot mark recipient's notification");

        // Stale version returns 412
        var staleAttempt = await MarkReadAsync(recipientClient, notification.Id, "stale-key", staleVersion);
        staleAttempt.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed, "stale version rejected");

        // First mark-read succeeds
        const string idempotencyKey = "rf1009-markread-001";
        var firstMark = await MarkReadAsync(recipientClient, notification.Id, idempotencyKey, version);
        firstMark.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await firstMark.Content.ReadFromJsonAsync<JsonElement>();
        firstBody.GetProperty("read").GetBoolean().Should().BeTrue();
        var newVersion = firstBody.GetProperty("version").GetString();
        newVersion.Should().NotBe(version, "version should change after mutation");

        // Replay with same key returns 200 OK (idempotent)
        var replay = await MarkReadAsync(recipientClient, notification.Id, idempotencyKey, version);
        replay.StatusCode.Should().Be(HttpStatusCode.OK, "replay with same key succeeds");

        // Conflict: same key but different version fingerprint returns 409
        var conflict = await MarkReadAsync(recipientClient, notification.Id, idempotencyKey, newVersion!);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict, "same key with different request fingerprint conflicts");

        // Verify SQL mutation: ReadAt set, RowVersion changed, single IdempotencyRecord
        await using var verify = _sql.CreateDbContext();
        var after = await CaptureNotificationAsync(verify, notification.Id);
        after.ReadAt.Should().NotBeNull("ReadAt should be set after mark-read");
        after.ReadAt.Should().BeAfter(baseline.OccurredAtUtc);
        after.RowVersion.Should().NotEqual(originalVersion, "RowVersion should change");
        after.Id.Should().Be(baseline.Id);
        after.RecipientUserId.Should().Be(baseline.RecipientUserId);
        after.SourceEntityType.Should().Be(baseline.SourceEntityType);
        after.SourceEntityId.Should().Be(baseline.SourceEntityId);
        after.Body.Should().Be(baseline.Body);

        var idempotencyRecords = await verify.IdempotencyRecords
            .Where(r => r.ActorUserId == recipient.Id && r.Operation == "NotificationRead" && r.IdempotencyKey == idempotencyKey)
            .CountAsync();
        idempotencyRecords.Should().Be(1, "exactly one idempotency record persisted");
    }

    [Fact(DisplayName = "RF-10-09-C01: outbox consumer replay returns durable notification without duplicate effect")]
    public async Task OutboxConsumer_Replay_ReturnsDurableNotificationWithoutDuplicate()
    {
        // Arrange: create outbox message and notification
        var recipient = await _sql.CreateUserAsync($"rf1009_consumer_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var sourceEntity = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var message = OutboxMessage.Create(
            messageId,
            "survey_request.assigned",
            DateTimeOffset.UtcNow,
            sourceEntity,
            "{\"surveyRequestId\":\"" + sourceEntity.ToString("N") + "\"}");

        await using (var setup = _sql.CreateDbContext())
        {
            setup.OutboxMessages.Add(message);
            await setup.SaveChangesAsync();
        }

        var notification = Notification.Create(
            Guid.NewGuid(),
            recipient.Id,
            "SurveyRequest",
            sourceEntity,
            "survey_request.assigned",
            "Survey assigned",
            "You have been assigned to a survey request.",
            DateTimeOffset.UtcNow);

        // Baseline: no notification or receipt yet
        await using (var baseline = _sql.CreateDbContext())
        {
            (await baseline.Notifications.CountAsync(n => n.Id == notification.Id)).Should().Be(0);
            (await baseline.ConsumerEffectReceipts.CountAsync(r => r.MessageId == messageId)).Should().Be(0);
        }

        // Act: consume twice with same message
        await using var context = _sql.CreateDbContext();
        var consumer = new NotificationOutboxConsumer(context, new ConsumerEffectService(context));

        var firstResult = await consumer.ConsumeAsync(messageId, notification);
        var replayResult = await consumer.ConsumeAsync(messageId, notification);

        // Assert: first returns Recorded, replay returns Replayed
        firstResult.Status.Should().Be(ConsumerEffectStatus.Recorded);
        firstResult.NotificationId.Should().Be(notification.Id);
        replayResult.Status.Should().Be(ConsumerEffectStatus.Replayed);
        replayResult.NotificationId.Should().Be(notification.Id);

        // Verify SQL: single notification, single receipt
        await using var verify = _sql.CreateDbContext();
        var notificationCount = await verify.Notifications.CountAsync(n => n.Id == notification.Id);
        notificationCount.Should().Be(1, "notification persisted exactly once");

        var receiptCount = await verify.ConsumerEffectReceipts
            .CountAsync(r => r.MessageId == messageId && r.ConsumerName == NotificationOutboxConsumer.ConsumerName);
        receiptCount.Should().Be(1, "receipt persisted exactly once");
    }

    [Fact(DisplayName = "RF-10-09-C01: producer creates outbox with correct correlation and message type")]
    public async Task Producer_CreateOutbox_LinksCorrectCorrelationAndType()
    {
        // Arrange: survey assignment scenario (known producer)
        var pm = await _sql.CreateUserAsync($"rf1009_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var operator1 = await _sql.CreateUserAsync($"rf1009_op1_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var operator2 = await _sql.CreateUserAsync($"rf1009_op2_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);

        Guid projectId;
        Guid requestId;
        Guid initialAssignmentId;
        await using (var setup = _sql.CreateDbContext())
        {
            projectId = await CreateProjectWithMemberAsync(setup, pm.Id);
            requestId = Guid.NewGuid();
            initialAssignmentId = Guid.NewGuid();

            var request = await CreateSurveyRequestAsync(setup, requestId, projectId, pm.Id);
            var initialAssignmentData = CreateSurveyAssignment(initialAssignmentId, requestId, operator1.Id, pm.Id);
            var initialAssignment = SurveyAssignment.Create(
                initialAssignmentData.Id,
                initialAssignmentData.SurveyRequestId,
                initialAssignmentData.OperatorUserId,
                initialAssignmentData.AssignedByUserId,
                initialAssignmentData.AssignedAt,
                acceptedAt: null,
                rejectedAt: null,
                rejectionReason: null,
                reassignmentReason: null,
                endedAt: null);
            setup.Add(request);
            setup.SurveyAssignments.Add(initialAssignment);
            await setup.SaveChangesAsync();
        }

        // Baseline: no outbox for this request yet
        await using (var baseline = _sql.CreateDbContext())
        {
            (await baseline.OutboxMessages.CountAsync(m => m.CorrelationId == requestId)).Should().Be(0);
        }

        // Act: perform reassignment (producer creates outbox)
        await using var context = _sql.CreateDbContext();
        var service = new SurveyAssignmentPersistenceService(context, new IdempotencyOperationService(context));
        var facts = await service.GetReassignmentFactsAsync(requestId);
        facts.Should().NotBeNull();

        var replacementData = CreateSurveyAssignment(Guid.NewGuid(), requestId, operator2.Id, pm.Id);
        var replacement = SurveyAssignment.Create(
            replacementData.Id,
            replacementData.SurveyRequestId,
            replacementData.OperatorUserId,
            replacementData.AssignedByUserId,
            replacementData.AssignedAt,
            acceptedAt: null,
            rejectedAt: null,
            rejectionReason: null,
            reassignmentReason: null,
            endedAt: null);

        var result = await service.ReassignAsync(
            requestId,
            facts!.ProjectId,
            facts.ActiveAssignmentId,
            replacement,
            pm.Id,
            DateTimeOffset.UtcNow,
            "Operator change for characterization test",
            "rf1009-producer-key",
            new string('f', 64));

        result.Status.Should().Be(IdempotencyOperationStatus.Executed);

        // Assert: outbox message created with correct fields
        await using var verify = _sql.CreateDbContext();
        var outbox = await verify.OutboxMessages
            .AsNoTracking()
            .Where(m => m.CorrelationId == requestId)
            .ToListAsync();

        outbox.Should().HaveCount(1, "producer creates single outbox message");
        var message = outbox.Single();
        message.MessageType.Should().Be("survey_request.reassigned");
        message.CorrelationId.Should().Be(requestId);
        message.DeliveryStatus.Should().Be(OutboxDeliveryStatus.Pending);
        message.DeliveryAttemptCount.Should().Be(0);
        message.PayloadJson.Should().Contain(requestId.ToString());
        message.PayloadJson.Should().Contain(result.PreviousAssignmentId.ToString());
        message.PayloadJson.Should().Contain(replacement.Id.ToString());
    }

    // Helper methods
    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password = "Current1!"
        });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }

    private static async Task<HttpResponseMessage> MarkReadAsync(HttpClient client, Guid notificationId, string key, string version)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/notifications/{notificationId}/read");
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return await client.SendAsync(request);
    }

    private static async Task<NotificationSnapshot> CaptureNotificationAsync(RoadGuardDbContext context, Guid notificationId)
    {
        var notification = await context.Notifications
            .AsNoTracking()
            .SingleAsync(n => n.Id == notificationId);

        return new NotificationSnapshot(
            notification.Id,
            notification.RecipientUserId,
            notification.SourceEntityType,
            notification.SourceEntityId,
            notification.EventType,
            notification.Title,
            notification.Body,
            notification.OccurredAtUtc,
            notification.ReadAt,
            notification.RowVersion.ToArray());
    }

    private static async Task<Guid> CreateProjectWithMemberAsync(RoadGuardDbContext context, Guid pmUserId)
    {
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            ProjectCode = $"RF1009-{Guid.NewGuid():N}",
            Name = "RF-10-09 Test Project",
            Status = ProjectStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
            CreatedAt = DateTimeOffset.UtcNow,
            RowVersion = new byte[8]
        };

        var member = ProjectMember.CreatePrimaryProjectManager(
            Guid.NewGuid(),
            projectId,
            pmUserId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)));

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        return projectId;
    }

    private static async Task<SurveyRequest> CreateSurveyRequestAsync(RoadGuardDbContext context, Guid requestId, Guid projectId, Guid pmUserId)
    {
        var roadSectionId = Guid.NewGuid();
        var roadSection = RoadSection.Create(roadSectionId, projectId, $"SEC-{Guid.NewGuid():N}", "Test Section");
        context.RoadSections.Add(roadSection);
        await context.SaveChangesAsync();

        return SurveyRequest.Create(
            requestId,
            projectId,
            roadSectionId,
            null, // surveyPlanId
            pmUserId,
            SurveyType.Original,
            SurveyRequestStatus.NewAssigned,
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddDays(7));
    }

    private sealed record SurveyAssignmentData(
        Guid Id,
        Guid SurveyRequestId,
        Guid OperatorUserId,
        Guid AssignedByUserId,
        DateTimeOffset AssignedAt);

    private static SurveyAssignmentData CreateSurveyAssignment(Guid assignmentId, Guid requestId, Guid operatorId, Guid assignedBy)
    {
        return new SurveyAssignmentData(
            assignmentId,
            requestId,
            operatorId,
            assignedBy,
            DateTimeOffset.UtcNow);
    }

    private sealed record NotificationSnapshot(
        Guid Id,
        Guid RecipientUserId,
        string SourceEntityType,
        Guid SourceEntityId,
        string EventType,
        string Title,
        string Body,
        DateTimeOffset OccurredAtUtc,
        DateTimeOffset? ReadAt,
        byte[] RowVersion);
}
