using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Sandbox;

// Authentication admission only. No claim about authenticated roles or full contracts.
[Collection(AuthenticationApiFixture.Name)]
public sealed class SandboxRouteAdmissionTests(AuthenticationSqlServerFixture sql)
{
    [Fact] public Task W02RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W02");
    [Fact] public Task W03RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W03");
    [Fact] public Task W04RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W04");
    [Fact] public Task W05RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W05");
    [Fact] public Task W06RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W06");
    [Fact] public Task W07RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W07");
    [Fact] public Task W08RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W08");
    [Fact] public Task W09RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W09");
    [Fact] public Task W10RejectsAnonymousAndInvalidBearer() => VerifyWorkflowAsync("W10");

    private async Task VerifyWorkflowAsync(string workflow)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var sentinel = new ActionSentinel();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services =>
            {
                services.AddSingleton<IStartupFilter, EndpointObserver>();
                services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(o => o.Filters.Add(sentinel));
            });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var before = await RowCountsAsync();
        foreach (var route in Routes.Where(r => r.Workflow == workflow))
        {
            var url = Regex.Replace(route.Template, @"\{([^}]+)\}", m => m.Groups[1].Value switch
            {
                "command" => "close",
                "layer" => "segments",
                _ => "11111111-1111-4111-8111-111111111111"
            });
            foreach (var invalidBearer in new[] { false, true })
            {
                using var request = new HttpRequestMessage(new HttpMethod(route.Method), url);
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                if (invalidBearer) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic.invalid.signature");
                using var response = await client.SendAsync(request);
                Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                    $"{route.Id} {route.Method} {route.Template}: expected 401, got {(int)response.StatusCode}");
                Assert.Equal(route.Controller + "." + route.Action, response.Headers.GetValues("X-Sbx-Endpoint").Single());
                Assert.Equal(route.Template, response.Headers.GetValues("X-Sbx-Route").Single());
                Assert.Equal("required", response.Headers.GetValues("X-Sbx-Authorization").Single());
            }
        }
        Assert.Equal(0, sentinel.Invocations);
        Assert.Equal(before, await RowCountsAsync());
    }

    private async Task<string[]> RowCountsAsync()
    {
        // Read-only exact row counts in this fixture-owned disposable database.
        sql.GetOwnedSeedDatabaseIdentity();
        await using var db = sql.CreateDbContext();
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id ORDER BY s.name,t.name";
        var names = new List<string>();
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) names.Add(reader.GetString(0));
        var result = new List<string>();
        foreach (var name in names)
        {
            cmd.CommandText = "SELECT COUNT_BIG(*) FROM " + name;
            result.Add(name + ":" + await cmd.ExecuteScalarAsync());
        }
        return result.ToArray();
    }

    private sealed class ActionSentinel : IActionFilter
    {
        public int Invocations { get; private set; }
        public void OnActionExecuting(ActionExecutingContext context) => Invocations++;
        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    private sealed class EndpointObserver : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, following) =>
            {
                context.Response.OnStarting(() =>
                {
                    var endpoint = context.GetEndpoint();
                    var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
                    if (action is not null)
                    {
                        context.Response.Headers["X-Sbx-Endpoint"] = action.ControllerTypeInfo.Name + "." + action.ActionName;
                        var template = (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? "";
                        template = template.Replace("v{version:apiVersion}", "v1", StringComparison.Ordinal);
                        context.Response.Headers["X-Sbx-Route"] = "/" + Regex.Replace(template.Trim('/'), @"\{(\w+):[^}]+\}", "{$1}");
                        context.Response.Headers["X-Sbx-Authorization"] = endpoint!.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0
                            && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null ? "required" : "missing";
                    }
                    return Task.CompletedTask;
                });
                await following();
            });
            next(app);
        };
    }

    private sealed record Route(string Id, string Workflow, string Method, string Template, string Controller, string Action);
    private static readonly Route[] Routes =
    [
        new("API-018", "W03", "GET", "/api/v1/cases", "CasesController", "List"),
        new("API-019", "W03", "GET", "/api/v1/cases/{caseId}", "CasesController", "Read"),
        new("API-020", "W03", "POST", "/api/v1/cases/{caseId}/conclusions", "CasesController", "Conclude"),
        new("API-021", "W03", "POST", "/api/v1/cases/{caseId}/publications", "CasesController", "Publish"),
        new("API-022", "W03", "POST", "/api/v1/cases/{caseId}/report-links", "CasesController", "Link"),
        new("API-023", "W03", "POST", "/api/v1/cases/{caseId}/report-splits", "CasesController", "Split"),
        new("API-024", "W03", "POST", "/api/v1/cases/{caseId}/triage", "CasesController", "Triage"),
        new("API-025", "W05", "GET", "/api/v1/datasets/{datasetId}/coverage", "SurveyV2Controller", "GetDatasetCoverage"),
        new("API-026", "W05", "GET", "/api/v1/datasets/{id}", "SurveyV2Controller", "ReadDataset"),
        new("API-027", "W05", "POST", "/api/v1/datasets/{id}/assessments", "SurveyAssessmentsController", "Create"),
        new("API-028", "W05", "GET", "/api/v1/datasets/{id}/assessments/{assessmentId}", "SurveyAssessmentsController", "Read"),
        new("API-029", "W05", "GET", "/api/v1/files/{fileId}", "UploadsController", "GetFileMetadata"),
        new("API-030", "W05", "GET", "/api/v1/files/{fileId}/content", "UploadsController", "Download"),
        new("API-031", "W05", "POST", "/api/v1/internal/processing-jobs/{jobId}/results", "ProcessingV2Controller", "ReceiveAiResult"),
        new("API-034", "W04", "POST", "/api/v1/labels/{labelId}/review", "TrainingLabelsController", "Review"),
        new("API-037", "W06", "GET", "/api/v1/me/inspection-tasks", "InspectionTasksController", "List"),
        new("API-038", "W05", "GET", "/api/v1/me/survey-tasks", "SurveyV2Controller", "ListMyTasks"),
        new("API-039", "W10", "GET", "/api/v1/notifications", "NotificationsController", "List"),
        new("API-040", "W10", "GET", "/api/v1/notifications/{notificationId}", "NotificationsController", "Get"),
        new("API-041", "W10", "POST", "/api/v1/notifications/{notificationId}/read", "NotificationsController", "MarkRead"),
        new("API-042", "W10", "GET", "/api/v1/notifications/{notificationId}/scope", "H6NotificationOperationsController", "Scope"),
        new("API-043", "W05", "POST", "/api/v1/offline/uploads", "UploadsController", "CreateOffline"),
        new("API-044", "W05", "POST", "/api/v1/processing-jobs", "ProcessingV2Controller", "CreateProcessingJob"),
        new("API-045", "W05", "GET", "/api/v1/processing-jobs/{jobId}", "ProcessingV2Controller", "GetProcessingJob"),
        new("API-046", "W05", "POST", "/api/v1/processing-jobs/{jobId}/retry", "ProcessingV2Controller", "RetryProcessingJob"),
        new("API-049", "W02", "GET", "/api/v1/projects", "ProjectsController", "List"),
        new("API-050", "W02", "POST", "/api/v1/projects", "ProjectsController", "Create"),
        new("API-051", "W02", "PUT", "/api/v1/projects/{projectId}", "ProjectsController", "Update"),
        new("API-052", "W05", "GET", "/api/v1/projects/{projectId}/ai-candidates/{detectionId}", "AiMockRunsController", "Candidate"),
        new("API-053", "W05", "POST", "/api/v1/projects/{projectId}/ai-mock-runs", "AiMockRunsController", "Create"),
        new("API-054", "W05", "GET", "/api/v1/projects/{projectId}/ai-mock-runs/{runId}", "AiMockRunsController", "Get"),
        new("API-055", "W05", "GET", "/api/v1/projects/{projectId}/ai-mock-runs/{runId}/result", "AiMockRunsController", "Result"),
        new("API-056", "W05", "GET", "/api/v1/projects/{projectId}/baseline-selections", "SurveyAssessmentsController", "Current"),
        new("API-057", "W05", "POST", "/api/v1/projects/{projectId}/baseline-selections", "SurveyAssessmentsController", "Select"),
        new("API-058", "W05", "GET", "/api/v1/projects/{projectId}/baseline-selections/{id}", "SurveyAssessmentsController", "History"),
        new("API-059", "W04", "POST", "/api/v1/projects/{projectId}/candidate-decisions", "CandidatesController", "Decide"),
        new("API-060", "W04", "GET", "/api/v1/projects/{projectId}/candidate-decisions/{decisionId}", "CandidatesController", "Read"),
        new("API-061", "W04", "GET", "/api/v1/projects/{projectId}/candidates", "CandidatesController", "Match"),
        new("API-062", "W10", "GET", "/api/v1/projects/{projectId}/clocks", "H6NotificationOperationsController", "Clocks"),
        new("API-063", "W10", "POST", "/api/v1/projects/{projectId}/clocks/{clockId}/appointments", "ClockDutyController", "Appoint"),
        new("API-064", "W10", "POST", "/api/v1/projects/{projectId}/clocks/{clockId}/extensions", "ClockDutyController", "Extend"),
        new("API-065", "W02", "GET", "/api/v1/projects/{projectId}/crs-profiles", "GeometryWorkflowController", "Profiles"),
        new("API-066", "W02", "POST", "/api/v1/projects/{projectId}/crs-profiles", "GeometryWorkflowController", "CreateProfile"),
        new("API-067", "W02", "GET", "/api/v1/projects/{projectId}/crs-profiles/{profileId}", "GeometryWorkflowController", "Profile"),
        new("API-068", "W10", "GET", "/api/v1/projects/{projectId}/defect-statistics", "DefectStatisticsController", "Read"),
        new("API-069", "W10", "POST", "/api/v1/projects/{projectId}/defect-statistics", "DefectStatisticsController", "Confirm"),
        new("API-070", "W04", "GET", "/api/v1/projects/{projectId}/defects", "DefectsController", "List"),
        new("API-071", "W04", "GET", "/api/v1/projects/{projectId}/defects/{defectId}", "DefectsController", "Read"),
        new("API-072", "W04", "POST", "/api/v1/projects/{projectId}/defects/{defectId}/assessments", "DefectsController", "Assess"),
        new("API-073", "W04", "POST", "/api/v1/projects/{projectId}/defects/{defectId}/verification-decisions", "DefectsController", "Verify"),
        new("API-074", "W10", "POST", "/api/v1/projects/{projectId}/exports", "ExportsController", "Create"),
        new("API-075", "W10", "GET", "/api/v1/projects/{projectId}/exports/{exportId}", "ExportsController", "Get"),
        new("API-076", "W10", "GET", "/api/v1/projects/{projectId}/exports/{exportId}/content", "ExportsController", "Content"),
        new("API-077", "W10", "GET", "/api/v1/projects/{projectId}/exports/{exportId}/manifest", "ExportsController", "Manifest"),
        new("API-078", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks", "FieldInspectionWorkflowController", "List"),
        new("API-079", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks", "FieldInspectionWorkflowController", "Create"),
        new("API-080", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}", "FieldInspectionWorkflowController", "Get"),
        new("API-081", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/accept", "FieldInspectionWorkflowController", "Accept"),
        new("API-082", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/assign", "FieldInspectionWorkflowController", "Assign"),
        new("API-083", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/cancel", "FieldInspectionWorkflowController", "Cancel"),
        new("API-084", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence-reuse", "FieldInspectionWorkflowController", "Reuse"),
        new("API-085", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}", "FieldInspectionWorkflowController", "Evidence"),
        new("API-086", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}/content", "FieldInspectionWorkflowController", "DownloadEvidence"),
        new("API-087", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/geometry", "FieldInspectionWorkflowController", "Geometry"),
        new("API-088", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/history", "FieldInspectionWorkflowController", "History"),
        new("API-089", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/location-impact-decisions", "FieldInspectionWorkflowController", "Impact"),
        new("API-090", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reassign", "FieldInspectionWorkflowController", "Reassign"),
        new("API-091", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reject", "FieldInspectionWorkflowController", "Reject"),
        new("API-092", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reviews", "FieldInspectionWorkflowController", "Review"),
        new("API-093", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start", "FieldInspectionWorkflowController", "Start"),
        new("API-094", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start-origin", "FieldInspectionWorkflowController", "Origin"),
        new("API-095", "W06", "POST", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions", "FieldInspectionWorkflowController", "Submit"),
        new("API-096", "W06", "GET", "/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}", "FieldInspectionWorkflowController", "Submission"),
        new("API-097", "W02", "GET", "/api/v1/projects/{projectId}/geometry-location-impacts", "PavementWorkflowController", "ListImpacts"),
        new("API-098", "W02", "POST", "/api/v1/projects/{projectId}/geometry-location-impacts", "PavementWorkflowController", "CreateImpact"),
        new("API-099", "W02", "GET", "/api/v1/projects/{projectId}/geometry-location-impacts/{impactId}", "PavementWorkflowController", "GetImpact"),
        new("API-100", "W02", "POST", "/api/v1/projects/{projectId}/geometry-location-impacts/{impactId}/decisions", "PavementWorkflowController", "DecideImpact"),
        new("API-101", "W02", "POST", "/api/v1/projects/{projectId}/geometry-map-publications", "PavementWorkflowController", "Publish"),
        new("API-102", "W02", "GET", "/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}", "PavementWorkflowController", "Manifest"),
        new("API-103", "W02", "GET", "/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}/layers/{layer}", "PavementWorkflowController", "Page"),
        new("API-104", "W02", "GET", "/api/v1/projects/{projectId}/geometry-package", "GeometryWorkflowController", "Package"),
        new("API-105", "W04", "GET", "/api/v1/projects/{projectId}/labels", "TrainingLabelsController", "List"),
        new("API-106", "W04", "POST", "/api/v1/projects/{projectId}/labels", "TrainingLabelsController", "Create"),
        new("API-107", "W04", "GET", "/api/v1/projects/{projectId}/labels/{labelId}", "TrainingLabelsController", "Read"),
        new("API-108", "W04", "POST", "/api/v1/projects/{projectId}/labels/{labelId}/revisions", "TrainingLabelsController", "Revise"),
        new("API-109", "W02", "GET", "/api/v1/projects/{projectId}/lifecycle", "ProjectLifecycleController", "Read"),
        new("API-110", "W02", "POST", "/api/v1/projects/{projectId}/lifecycle/{command}", "ProjectLifecycleController", "Execute"),
        new("API-111", "W02", "POST", "/api/v1/projects/{projectId}/lifecycle/renewed-handling-scope", "ProjectLifecycleController", "Renew"),
        new("API-113", "W09", "POST", "/api/v1/projects/{projectId}/offline/artifacts", "OfflineWorkflowController", "RegisterArtifact"),
        new("API-114", "W09", "GET", "/api/v1/projects/{projectId}/offline/artifacts/{artifactId}", "OfflineWorkflowController", "GetArtifact"),
        new("API-115", "W09", "GET", "/api/v1/projects/{projectId}/offline/batches/{batchId}", "OfflineWorkflowController", "GetBatch"),
        new("API-116", "W09", "POST", "/api/v1/projects/{projectId}/offline/devices", "OfflineWorkflowController", "RegisterDevice"),
        new("API-117", "W09", "GET", "/api/v1/projects/{projectId}/offline/devices/{registrationId}", "OfflineWorkflowController", "GetDevice"),
        new("API-118", "W09", "POST", "/api/v1/projects/{projectId}/offline/devices/{registrationId}/revoke", "OfflineWorkflowController", "RevokeDevice"),
        new("API-119", "W09", "POST", "/api/v1/projects/{projectId}/offline/grants", "OfflineWorkflowController", "IssueGrant"),
        new("API-120", "W09", "GET", "/api/v1/projects/{projectId}/offline/grants/{grantId}", "OfflineWorkflowController", "GetGrant"),
        new("API-121", "W09", "POST", "/api/v1/projects/{projectId}/offline/grants/{grantId}/revoke", "OfflineWorkflowController", "RevokeGrant"),
        new("API-122", "W09", "POST", "/api/v1/projects/{projectId}/offline/import", "OfflineWorkflowController", "ImportPackage"),
        new("API-123", "W09", "GET", "/api/v1/projects/{projectId}/offline/origins/{originId}", "OfflineWorkflowController", "GetOrigin"),
        new("API-124", "W09", "GET", "/api/v1/projects/{projectId}/offline/packages/{packageId}", "OfflineWorkflowController", "GetPackage"),
        new("API-125", "W09", "POST", "/api/v1/projects/{projectId}/offline/packages/export", "OfflineWorkflowController", "ExportPackage"),
        new("API-126", "W09", "POST", "/api/v1/projects/{projectId}/offline/packages/prepare", "OfflineWorkflowController", "PreparePackage"),
        new("API-127", "W09", "POST", "/api/v1/projects/{projectId}/offline/snapshots", "OfflineWorkflowController", "CaptureSnapshot"),
        new("API-128", "W09", "GET", "/api/v1/projects/{projectId}/offline/snapshots/{snapshotId}", "OfflineWorkflowController", "GetSnapshot"),
        new("API-129", "W09", "POST", "/api/v1/projects/{projectId}/offline/starts/reconcile", "OfflineWorkflowController", "ReconcileStart"),
        new("API-130", "W09", "POST", "/api/v1/projects/{projectId}/offline/sync", "OfflineWorkflowController", "Synchronize"),
        new("API-131", "W02", "GET", "/api/v1/projects/{projectId}/pavement-layouts/{layoutId}", "PavementWorkflowController", "GetLayout"),
        new("API-132", "W02", "POST", "/api/v1/projects/{projectId}/pavement-layouts/as-built", "PavementWorkflowController", "CreateAsBuilt"),
        new("API-134", "W02", "PUT", "/api/v1/projects/{projectId}/primary-project-manager", "ProjectsController", "ReassignPrimaryProjectManager"),
        new("API-135", "W10", "GET", "/api/v1/projects/{projectId}/receiving-requests", "BusinessDutyController", "List"),
        new("API-136", "W10", "GET", "/api/v1/projects/{projectId}/receiving-requests/{requestId}", "BusinessDutyController", "Read"),
        new("API-137", "W10", "POST", "/api/v1/projects/{projectId}/receiving-requests/{requestId}/ack", "BusinessDutyController", "Ack"),
        new("API-138", "W10", "POST", "/api/v1/projects/{projectId}/receiving-requests/{requestId}/appointment", "BusinessDutyController", "Appoint"),
        new("API-139", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages", "RepairPackagesController", "Create"),
        new("API-140", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items", "RepairPackagesController", "Propose"),
        new("API-141", "W07", "GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}", "RepairsController", "Item"),
        new("API-142", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/approve", "RepairPackagesController", "Approve"),
        new("API-143", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/assign", "RepairPackagesController", "Assign"),
        new("API-144", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/cancellations", "RepairLifecycleController", "Cancel"),
        new("API-145", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/corrections", "RepairsController", "Correct"),
        new("API-146", "W08", "GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/eligibility", "RepairEligibilityController", "Get"),
        new("API-147", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/final-confirmation", "RepairPackagesController", "ConfirmFinal"),
        new("API-148", "W07", "GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/history", "RepairsController", "History"),
        new("API-149", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/normal-successors", "RepairLifecycleController", "ContinueNormally"),
        new("API-150", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/review-requests", "RepairsController", "RequestReview"),
        new("API-151", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures", "RepairSafetyController", "Create"),
        new("API-152", "W07", "GET", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}", "RepairSafetyController", "Read"),
        new("API-153", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/checks", "RepairSafetyController", "Check"),
        new("API-154", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/installation", "RepairSafetyController", "Install"),
        new("API-155", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/warnings/{warningId}/acknowledgement", "RepairSafetyController", "Acknowledge"),
        new("API-156", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/assessment", "RepairExecutionController", "Assess"),
        new("API-157", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts", "RepairExecutionController", "SubmitAttempt"),
        new("API-158", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/reviews", "RepairExecutionController", "ReviewAttempt"),
        new("API-159", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/supplements", "RepairExecutionController", "SupplementAttempt"),
        new("API-160", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-finish", "RepairExecutionController", "Finish"),
        new("API-161", "W07", "POST", "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-start", "RepairExecutionController", "Start"),
        new("API-162", "W08", "POST", "/api/v1/projects/{projectId}/repair-policies/drafts", "RepairPoliciesController", "Create"),
        new("API-163", "W08", "GET", "/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}", "RepairPoliciesController", "Draft"),
        new("API-164", "W08", "PUT", "/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}", "RepairPoliciesController", "Update"),
        new("API-165", "W08", "POST", "/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}/publish", "RepairPoliciesController", "Publish"),
        new("API-166", "W08", "GET", "/api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}", "RepairPoliciesController", "Revision"),
        new("API-167", "W08", "POST", "/api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}/revoke", "RepairPoliciesController", "Revoke"),
        new("API-168", "W10", "GET", "/api/v1/projects/{projectId}/reports/items", "ReportingController", "Items"),
        new("API-169", "W10", "GET", "/api/v1/projects/{projectId}/reports/summary", "ReportingController", "Summary"),
        new("API-170", "W10", "GET", "/api/v1/projects/{projectId}/reports/timeline", "ReportingController", "Timeline"),
        new("API-171", "W10", "POST", "/api/v1/projects/{projectId}/retention/evaluations", "RetentionController", "Evaluate"),
        new("API-172", "W10", "GET", "/api/v1/projects/{projectId}/retention/evaluations/{id}", "RetentionController", "Evaluation"),
        new("API-173", "W10", "GET", "/api/v1/projects/{projectId}/retention/files/{fileId}", "RetentionController", "File"),
        new("API-174", "W10", "PUT", "/api/v1/projects/{projectId}/retention/files/{fileId}/basis", "RetentionController", "Basis"),
        new("API-175", "W08", "GET", "/api/v1/projects/{projectId}/road-coverage", "RoadCoverageController", "Read"),
        new("API-176", "W08", "POST", "/api/v1/projects/{projectId}/road-coverage", "RoadCoverageController", "Confirm"),
        new("API-177", "W02", "POST", "/api/v1/projects/{projectId}/road-geometry-drafts", "GeometryWorkflowController", "CreateDraft"),
        new("API-178", "W02", "GET", "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}", "GeometryWorkflowController", "Draft"),
        new("API-179", "W02", "PUT", "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}", "GeometryWorkflowController", "EditDraft"),
        new("API-180", "W02", "POST", "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/confirm", "GeometryWorkflowController", "Confirm"),
        new("API-181", "W02", "POST", "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/preview", "GeometryWorkflowController", "Preview"),
        new("API-182", "W02", "GET", "/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/readiness", "GeometryWorkflowController", "Readiness"),
        new("API-183", "W02", "POST", "/api/v1/projects/{projectId}/road-sections", "ProjectRoadSectionsController", "Create"),
        new("API-184", "W02", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts", "GeometryWorkflowController", "CreateDraft"),
        new("API-185", "W02", "GET", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}", "GeometryWorkflowController", "Draft"),
        new("API-186", "W02", "PUT", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}", "GeometryWorkflowController", "EditDraft"),
        new("API-187", "W02", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/confirm", "GeometryWorkflowController", "Confirm"),
        new("API-188", "W02", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/preview", "GeometryWorkflowController", "Preview"),
        new("API-189", "W05", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans", "SurveyPlanningController", "CreatePlan"),
        new("API-190", "W05", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-requests", "SurveyPlanningController", "CreateRequest"),
        new("API-191", "W02", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions", "ProjectRoadSectionsController", "CreateVersion"),
        new("API-192", "W02", "GET", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/geometry", "GeometryWorkflowController", "Geometry"),
        new("API-193", "W02", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets", "GeometryWorkflowController", "CreateSet"),
        new("API-194", "W02", "GET", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}", "GeometryWorkflowController", "Set"),
        new("API-195", "W02", "PUT", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}", "GeometryWorkflowController", "EditSet"),
        new("API-196", "W02", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}/publish", "GeometryWorkflowController", "Publish"),
        new("API-197", "W02", "POST", "/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/preview", "GeometryWorkflowController", "PreviewSet"),
        new("API-198", "W02", "GET", "/api/v1/projects/{projectId}/route-systems", "GeometryWorkflowController", "Systems"),
        new("API-199", "W02", "POST", "/api/v1/projects/{projectId}/route-systems", "GeometryWorkflowController", "CreateSystem"),
        new("API-200", "W02", "POST", "/api/v1/projects/{projectId}/route-versions/{routeVersionId}/segment-sets/{segmentSetId}/pavement-plans", "PavementWorkflowController", "CreatePlan"),
        new("API-201", "W05", "POST", "/api/v1/projects/{projectId}/survey-plans", "SurveyV2Controller", "CreatePlan"),
        new("API-202", "W05", "POST", "/api/v1/projects/{projectId}/survey-plans/{surveyPlanId}/postpone", "SurveyPlanningController", "PostponePlan"),
        new("API-203", "W05", "POST", "/api/v1/projects/{projectId}/survey-tasks", "SurveyV2Controller", "CreateTask"),
        new("API-204", "W05", "POST", "/api/v1/projects/{projectId}/validation-runs", "ProcessingV2Controller", "CreateValidationRun"),
        new("API-205", "W02", "POST", "/api/v1/projects/{projectId}/warranties", "ProjectWarrantiesController", "Create"),
        new("API-206", "W10", "GET", "/api/v1/projects/{projectId}/weekly-digests/{digestId}", "H6NotificationOperationsController", "WeeklyDigest"),
        new("API-207", "W02", "GET", "/api/v1/projects/{projectId}/work-package", "ProjectWorkPackagesController", "Get"),
        new("API-208", "W03", "GET", "/api/v1/reporter-evidence/files/{fileId}", "ReporterEvidenceController", "GetFileMetadata"),
        new("API-209", "W03", "GET", "/api/v1/reporter-evidence/files/{fileId}/content", "ReporterEvidenceController", "Download"),
        new("API-210", "W03", "GET", "/api/v1/reporter-evidence/publications/{publicationId}/reports/{reportId}/evidence/{evidenceId}/content", "ReporterEvidenceController", "DownloadPublication"),
        new("API-211", "W03", "POST", "/api/v1/reporter-evidence/uploads", "ReporterEvidenceController", "Create"),
        new("API-212", "W03", "GET", "/api/v1/reporter-evidence/uploads/{uploadId}", "ReporterEvidenceController", "GetSession"),
        new("API-213", "W03", "POST", "/api/v1/reporter-evidence/uploads/{uploadId}/complete", "ReporterEvidenceController", "Complete"),
        new("API-214", "W03", "POST", "/api/v1/reporter-evidence/uploads/{uploadId}/part-urls", "ReporterEvidenceController", "GetPartUrls"),
        new("API-215", "W03", "GET", "/api/v1/reports", "ReportsController", "List"),
        new("API-216", "W03", "POST", "/api/v1/reports", "ReportsController", "Create"),
        new("API-217", "W03", "GET", "/api/v1/reports/{reportId}", "ReportsController", "Read"),
        new("API-218", "W03", "GET", "/api/v1/reports/{reportId}/evidence/{evidenceId}/download", "ReportsController", "Download"),
        new("API-219", "W03", "POST", "/api/v1/reports/{reportId}/supplements", "ReportsController", "Supplement"),
        new("API-220", "W10", "POST", "/api/v1/retention/holds", "RetentionController", "CreateHold"),
        new("API-221", "W10", "GET", "/api/v1/retention/holds/{id}", "RetentionController", "Hold"),
        new("API-222", "W10", "POST", "/api/v1/retention/holds/{id}/release", "RetentionController", "Release"),
        new("API-223", "W05", "GET", "/api/v1/survey-plans/{id}", "SurveyV2Controller", "ReadPlan"),
        new("API-224", "W05", "POST", "/api/v1/survey-plans/{planId}/postpone", "SurveyV2Controller", "Postpone"),
        new("API-225", "W05", "GET", "/api/v1/survey-tasks/{id}/work-package", "SurveyV2Controller", "ReadWorkPackage"),
        new("API-226", "W05", "GET", "/api/v1/survey-tasks/{taskId}", "SurveyV2Controller", "GetTask"),
        new("API-227", "W05", "POST", "/api/v1/survey-tasks/{taskId}/accept", "SurveyV2Controller", "AcceptTask"),
        new("API-228", "W05", "POST", "/api/v1/survey-tasks/{taskId}/cancel", "SurveyV2Controller", "CancelTask"),
        new("API-229", "W05", "POST", "/api/v1/survey-tasks/{taskId}/datasets", "SurveyV2Controller", "SubmitDataset"),
        new("API-230", "W05", "POST", "/api/v1/survey-tasks/{taskId}/decline", "SurveyV2Controller", "DeclineTask"),
        new("API-231", "W05", "POST", "/api/v1/survey-tasks/{taskId}/reassign", "SurveyV2Controller", "ReassignTask"),
        new("API-232", "W05", "POST", "/api/v1/survey-tasks/{taskId}/supplements", "SurveyV2Controller", "RequestSupplement"),
        new("API-233", "W05", "POST", "/api/v1/uploads", "UploadsController", "Create"),
        new("API-234", "W05", "GET", "/api/v1/uploads/{uploadId}", "UploadsController", "GetSession"),
        new("API-235", "W05", "POST", "/api/v1/uploads/{uploadId}/complete", "UploadsController", "Complete"),
        new("API-236", "W05", "POST", "/api/v1/uploads/{uploadId}/part-urls", "UploadsController", "GetPartUrls"),
        new("API-241", "W05", "GET", "/api/v1/validation-runs/{runId}", "ProcessingV2Controller", "GetValidationResult"),
    ];
}
