using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Creates deterministic, non-production domain fixtures for local Postman scenarios.
/// Real recipient addresses are deliberately excluded from database seeding.
/// </summary>
public sealed class PostmanScenarioSeedStep : ISeedStep
{
    public static readonly Guid ProjectId = FixtureId("0001");
    public static readonly Guid PrimaryMembershipId = FixtureId("0002");
    public static readonly Guid HandoverDocumentId = FixtureId("0003");
    public static readonly Guid RoadSectionId = FixtureId("0004");
    public static readonly Guid RoadSectionVersionId = FixtureId("0005");
    public static readonly Guid SegmentSetId = FixtureId("0006");
    public static readonly Guid SegmentOneId = FixtureId("0007");
    public static readonly Guid SegmentTwoId = FixtureId("0008");
    public static readonly Guid SurveyPlanId = FixtureId("0009");
    public static readonly Guid SurveyRequestId = FixtureId("000a");
    public static readonly Guid SurveyId = FixtureId("000b");
    public static readonly Guid SurveyAssignmentId = FixtureId("000c");
    public static readonly Guid DefectId = FixtureId("000d");
    public static readonly Guid FieldInspectionTaskId = FixtureId("0010");
    public static readonly Guid FieldInspectionAssignmentId = FixtureId("0011");
    public static readonly Guid OperatorMembershipId = FixtureId("0012");
    public static readonly Guid RepairCrewMembershipId = FixtureId("0013");
    public static readonly Guid SurveyPlanScopeId = FixtureId("0014");
    public static readonly Guid SurveyRequestScopeId = FixtureId("0015");

    public const string ProjectCode = "RG-POSTMAN-001";
    public const string RoadSectionCode = "RG-POSTMAN-RS-001";
    public const string DefectTypeCode = "RG_POSTMAN_POTHOLE";
    public const string FieldInspectionTaskCode = "RG-POSTMAN-FI-001";

    public int Order => 40;

    public string Name => nameof(PostmanScenarioSeedStep);

    public async Task SeedAsync(
        RoadGuardDbContext context,
        CancellationToken cancellationToken = default)
        => await SeedCoreAsync(context, allowDuplicateRetry: true, cancellationToken);

    private static async Task SeedCoreAsync(
        RoadGuardDbContext context,
        bool allowDuplicateRetry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var users = await context.Users
            .Where(user =>
                user.NormalizedEmail == PostmanUserSeedStep.NormalizedProjectManagerEmail ||
                user.NormalizedEmail == PostmanUserSeedStep.NormalizedOperatorEmail ||
                user.NormalizedEmail == PostmanUserSeedStep.NormalizedRepairCrewEmail)
            .ToDictionaryAsync(user => user.NormalizedEmail!, cancellationToken);
        var projectManagerId = RequireUser(users, PostmanUserSeedStep.ProjectManagerEmail);
        var operatorId = RequireUser(users, PostmanUserSeedStep.OperatorEmail);
        var repairCrewId = RequireUser(users, PostmanUserSeedStep.RepairCrewEmail);

        await ValidateExistingFixtureAsync(context, projectManagerId, operatorId, repairCrewId, cancellationToken);

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 32648);
        var fixtureDate = new DateOnly(2026, 9, 1);
        var fixtureTime = new DateTimeOffset(2026, 9, 1, 1, 0, 0, TimeSpan.Zero);

        if (!await context.Projects.AnyAsync(item => item.Id == ProjectId, cancellationToken))
        {
            context.Projects.Add(Project.Create(
                ProjectId,
                ProjectCode,
                "Postman road maintenance project",
                "Development-only fixture for local API scenarios.",
                32648,
                fixtureDate,
                fixtureDate.AddYears(1),
                fixtureTime));
        }

        await AddMembershipIfMissingAsync(
            context,
            PrimaryMembershipId,
            projectManagerId,
            UserRoleCode.ProjectManager,
            isPrimary: true,
            fixtureDate,
            cancellationToken);
        await AddMembershipIfMissingAsync(
            context,
            OperatorMembershipId,
            operatorId,
            UserRoleCode.DroneOperator,
            isPrimary: false,
            fixtureDate,
            cancellationToken);
        await AddMembershipIfMissingAsync(
            context,
            RepairCrewMembershipId,
            repairCrewId,
            UserRoleCode.RepairCrew,
            isPrimary: false,
            fixtureDate,
            cancellationToken);

        if (!await context.HandoverDocuments.AnyAsync(item => item.Id == HandoverDocumentId, cancellationToken))
        {
            context.HandoverDocuments.Add(HandoverDocument.Create(
                HandoverDocumentId,
                ProjectId,
                "RG-POSTMAN-HD-001",
                fixtureDate,
                projectManagerId,
                fileId: null,
                "Development-only Postman fixture."));
        }

        if (!await context.RoadSections.AnyAsync(item => item.Id == RoadSectionId, cancellationToken))
        {
            context.RoadSections.Add(RoadSection.Create(
                RoadSectionId,
                ProjectId,
                RoadSectionCode,
                "Postman section 1"));
        }

        if (!await context.RoadSectionVersions.AnyAsync(item => item.Id == RoadSectionVersionId, cancellationToken))
        {
            context.RoadSectionVersions.Add(RoadSectionVersion.Create(
                RoadSectionVersionId,
                RoadSectionId,
                versionNo: 1,
                isCurrent: true,
                geometryFactory.CreateLineString(
                [
                    new Coordinate(500000, 1100000),
                    new Coordinate(500100, 1100100),
                    new Coordinate(500200, 1100150)
                ]),
                fixtureTime,
                "Initial Postman fixture alignment"));
        }

        if (!await context.RoadSegmentSets.AnyAsync(item => item.Id == SegmentSetId, cancellationToken))
        {
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(SegmentSetId, RoadSectionVersionId, "PUBLISHED"));
        }

        if (!await context.RoadSegments.AnyAsync(item => item.Id == SegmentOneId, cancellationToken))
        {
            context.RoadSegments.Add(RoadSegment.Create(SegmentOneId, SegmentSetId, RoadSectionVersionId, 1));
        }

        if (!await context.RoadSegments.AnyAsync(item => item.Id == SegmentTwoId, cancellationToken))
        {
            context.RoadSegments.Add(RoadSegment.Create(SegmentTwoId, SegmentSetId, RoadSectionVersionId, 2));
        }

        if (!await context.SurveyPlans.AnyAsync(item => item.Id == SurveyPlanId, cancellationToken))
        {
            context.SurveyPlans.Add(SurveyPlan.Create(
                SurveyPlanId,
                ProjectId,
                RoadSectionId,
                fixtureTime.AddDays(7),
                fixtureTime.AddDays(7).AddHours(2),
                SurveyType.Periodic,
                SurveyPlanStatus.Planned,
                "{\"formats\":[\"video\",\"srt\"]}",
                RoadSectionVersionId));
        }

        if (!await context.SurveyRequests.AnyAsync(item => item.Id == SurveyRequestId, cancellationToken))
        {
            context.SurveyRequests.Add(SurveyRequest.Create(
                SurveyRequestId,
                ProjectId,
                RoadSectionId,
                SurveyPlanId,
                projectManagerId,
                SurveyType.Periodic,
                SurveyRequestStatus.NewAssigned,
                fixtureTime,
                fixtureTime.AddDays(7),
                "{\"formats\":[\"video\",\"srt\"]}",
                RoadSectionVersionId));
        }

        if (!await context.SurveyAssignments.AnyAsync(item => item.Id == SurveyAssignmentId, cancellationToken))
        {
            context.SurveyAssignments.Add(SurveyAssignment.Create(
                SurveyAssignmentId,
                SurveyRequestId,
                operatorId,
                projectManagerId,
                fixtureTime.AddHours(1),
                acceptedAt: fixtureTime.AddHours(2),
                rejectedAt: null,
                rejectionReason: null,
                reassignmentReason: null,
                endedAt: null));
        }

        if (!await context.Surveys.AnyAsync(item => item.Id == SurveyId, cancellationToken))
        {
            context.Surveys.Add(Survey.Create(
                SurveyId,
                surveyRequestId: null,
                ProjectId,
                RoadSectionVersionId,
                SurveyType.Periodic,
                SurveyStatus.Completed,
                isBaselineConfirmed: false,
                baselineConfirmedByUserId: null,
                baselineConfirmedAt: null));
        }

        if (!await context.SurveyPlanScopes.AnyAsync(item => item.Id == SurveyPlanScopeId, cancellationToken))
        {
            context.SurveyPlanScopes.Add(SurveyPlanScope.Create(
                SurveyPlanScopeId,
                SurveyPlanId,
                RoadSectionVersionId,
                SegmentSetId,
                $"[\"{SegmentOneId}\",\"{SegmentTwoId}\"]",
                "SURFACE"));
        }

        if (!await context.SurveyRequestScopes.AnyAsync(item => item.Id == SurveyRequestScopeId, cancellationToken))
        {
            context.SurveyRequestScopes.Add(SurveyRequestScope.Create(
                SurveyRequestScopeId,
                SurveyRequestId,
                RoadSectionVersionId,
                SegmentSetId,
                $"[\"{SegmentOneId}\",\"{SegmentTwoId}\"]",
                "SURFACE"));
        }

        if (!await context.DefectTypes.AnyAsync(item => item.Code == DefectTypeCode, cancellationToken))
        {
            context.DefectTypes.Add(DefectType.Create(
                DefectTypeCode,
                "Postman pothole",
                "Development-only defect type fixture."));
        }

        if (!await context.Defects.AnyAsync(item => item.Id == DefectId, cancellationToken))
        {
            context.Defects.Add(Defect.Create(
                DefectId,
                ProjectId,
                RoadSectionVersionId,
                sourceAIDetectionId: null,
                DefectTypeCode,
                causeCategoryCode: null,
                DefectSeverity.Medium,
                DefectStatus.Open,
                geometryFactory.CreatePoint(new Coordinate(500050, 1100050)),
                fixtureTime.AddDays(1)));
        }

        if (!await context.FieldInspectionTasks.AnyAsync(item => item.Id == FieldInspectionTaskId, cancellationToken))
        {
            context.FieldInspectionTasks.Add(FieldInspectionTask.Create(
                FieldInspectionTaskId,
                FieldInspectionTaskCode,
                ProjectId,
                DefectId,
                SurveyId,
                RoadSectionVersionId,
                requiredMeasurementType: 1,
                "{\"segmentIds\":[\"6a4dbd16-a646-46da-9e5c-200000000007\"]}",
                "Measure pothole dimensions and attach evidence.",
                missingInformation: null,
                fixtureTime.AddDays(14),
                FieldInspectionTaskStatus.NewAssigned,
                projectManagerId,
                reviewDecision: null,
                reviewedByUserId: null,
                reviewedAt: null,
                reviewReason: null));
        }

        if (!await context.FieldInspectionAssignments.AnyAsync(item => item.Id == FieldInspectionAssignmentId, cancellationToken))
        {
            context.FieldInspectionAssignments.Add(FieldInspectionAssignment.Create(
                FieldInspectionAssignmentId,
                FieldInspectionTaskId,
                repairCrewId,
                projectManagerId,
                fixtureTime.AddDays(2),
                endedAt: null,
                FieldInspectionAssignmentStatus.Active,
                reason: null));
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (allowDuplicateRetry && IsDuplicateKey(exception))
        {
            context.ChangeTracker.Clear();
            await SeedCoreAsync(context, allowDuplicateRetry: false, cancellationToken);
        }
    }

    private static async Task AddMembershipIfMissingAsync(
        RoadGuardDbContext context,
        Guid membershipId,
        Guid userId,
        UserRoleCode role,
        bool isPrimary,
        DateOnly validFrom,
        CancellationToken cancellationToken)
    {
        if (await context.ProjectMembers.AnyAsync(item => item.Id == membershipId, cancellationToken))
        {
            return;
        }

        context.ProjectMembers.Add(new ProjectMember
        {
            Id = membershipId,
            ProjectId = ProjectId,
            UserId = userId,
            RoleCode = role,
            IsPrimary = isPrimary,
            ValidFrom = validFrom,
            Status = ProjectMemberStatus.Active
        });
    }

    private static async Task ValidateExistingFixtureAsync(
        RoadGuardDbContext context,
        Guid projectManagerId,
        Guid operatorId,
        Guid repairCrewId,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == ProjectId, cancellationToken);
        if (project is not null && !string.Equals(project.ProjectCode, ProjectCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Postman fixture collision: project {ProjectId} belongs to code '{project.ProjectCode}', expected '{ProjectCode}'.");
        }

        var expectedMembers = new[]
        {
            (PrimaryMembershipId, projectManagerId, UserRoleCode.ProjectManager, true),
            (OperatorMembershipId, operatorId, UserRoleCode.DroneOperator, false),
            (RepairCrewMembershipId, repairCrewId, UserRoleCode.RepairCrew, false)
        };
        foreach (var (id, userId, role, primary) in expectedMembers)
        {
            var member = await context.ProjectMembers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (member is not null &&
                (member.ProjectId != ProjectId || member.UserId != userId || member.RoleCode != role ||
                 member.IsPrimary != primary || member.Status != ProjectMemberStatus.Active))
            {
                throw new InvalidOperationException(
                    $"Postman fixture collision: membership {id} has different project/user/role/status ownership.");
            }
        }

        var segmentSet = await context.RoadSegmentSets.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == SegmentSetId, cancellationToken);
        if (segmentSet is not null &&
            (segmentSet.RoadSectionVersionId != RoadSectionVersionId || segmentSet.Status != "PUBLISHED"))
        {
            throw new InvalidOperationException(
                $"Postman fixture collision: segment set {SegmentSetId} has incompatible route/status.");
        }

        var section = await context.RoadSections.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == RoadSectionId, cancellationToken);
        EnsureOwnership(section is null ||
                        section.ProjectId == ProjectId && section.Code == RoadSectionCode,
            RoadSectionId, "road section");

        var sectionVersion = await context.RoadSectionVersions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == RoadSectionVersionId, cancellationToken);
        EnsureOwnership(sectionVersion is null ||
                        sectionVersion.RoadSectionId == RoadSectionId && sectionVersion.VersionNo == 1,
            RoadSectionVersionId, "road section version");

        var handover = await context.HandoverDocuments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == HandoverDocumentId, cancellationToken);
        EnsureOwnership(handover is null ||
                        handover.ProjectId == ProjectId && handover.AcceptedByUserId == projectManagerId &&
                        handover.DocumentNo == "RG-POSTMAN-HD-001",
            HandoverDocumentId, "handover document");

        var segments = await context.RoadSegments.AsNoTracking()
            .Where(item => item.Id == SegmentOneId || item.Id == SegmentTwoId)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        EnsureOwnership(!segments.TryGetValue(SegmentOneId, out var segmentOne) ||
                        segmentOne.SegmentSetId == SegmentSetId && segmentOne.RoadSectionVersionId == RoadSectionVersionId && segmentOne.Sequence == 1,
            SegmentOneId, "road segment");
        EnsureOwnership(!segments.TryGetValue(SegmentTwoId, out var segmentTwo) ||
                        segmentTwo.SegmentSetId == SegmentSetId && segmentTwo.RoadSectionVersionId == RoadSectionVersionId && segmentTwo.Sequence == 2,
            SegmentTwoId, "road segment");

        var plan = await context.SurveyPlans.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == SurveyPlanId, cancellationToken);
        EnsureOwnership(plan is null ||
                        plan.ProjectId == ProjectId && plan.RoadSectionId == RoadSectionId &&
                        plan.RoadSectionVersionId == RoadSectionVersionId,
            SurveyPlanId, "survey plan");

        var request = await context.SurveyRequests.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == SurveyRequestId, cancellationToken);
        EnsureOwnership(request is null ||
                        request.ProjectId == ProjectId && request.RoadSectionId == RoadSectionId &&
                        request.RoadSectionVersionId == RoadSectionVersionId && request.SurveyPlanId == SurveyPlanId &&
                        request.RequestedByUserId == projectManagerId,
            SurveyRequestId, "survey request");

        var assignment = await context.SurveyAssignments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == SurveyAssignmentId, cancellationToken);
        EnsureOwnership(assignment is null ||
                        assignment.SurveyRequestId == SurveyRequestId && assignment.OperatorUserId == operatorId &&
                        assignment.AssignedByUserId == projectManagerId && assignment.EndedAt is null,
            SurveyAssignmentId, "survey assignment");

        var survey = await context.Surveys.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == SurveyId, cancellationToken);
        EnsureOwnership(survey is null ||
                        survey.SurveyRequestId is null && survey.ProjectId == ProjectId &&
                        survey.RoadSectionVersionId == RoadSectionVersionId,
            SurveyId, "survey");

        var defect = await context.Defects.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == DefectId, cancellationToken);
        EnsureOwnership(defect is null ||
                        defect.ProjectId == ProjectId && defect.RoadSectionVersionId == RoadSectionVersionId &&
                        defect.DefectTypeCode == DefectTypeCode,
            DefectId, "defect");

        var task = await context.FieldInspectionTasks.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == FieldInspectionTaskId, cancellationToken);
        EnsureOwnership(task is null ||
                        task.ProjectId == ProjectId && task.DefectId == DefectId && task.SurveyId == SurveyId &&
                        task.RoadSectionVersionId == RoadSectionVersionId && task.TaskCode == FieldInspectionTaskCode,
            FieldInspectionTaskId, "field inspection task");

        var fieldAssignment = await context.FieldInspectionAssignments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == FieldInspectionAssignmentId, cancellationToken);
        EnsureOwnership(fieldAssignment is null ||
                        fieldAssignment.FieldInspectionTaskId == FieldInspectionTaskId &&
                        fieldAssignment.AssignedToUserId == repairCrewId &&
                        fieldAssignment.AssignedByUserId == projectManagerId &&
                        fieldAssignment.Status == FieldInspectionAssignmentStatus.Active,
            FieldInspectionAssignmentId, "field inspection assignment");

        var planScope = await context.SurveyPlanScopes.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == SurveyPlanScopeId, cancellationToken);
        if (planScope is not null &&
            (planScope.SurveyPlanId != SurveyPlanId || planScope.RouteSectionVersionId != RoadSectionVersionId ||
             planScope.SegmentSetId != SegmentSetId || planScope.TargetBand != "SURFACE"))
        {
            throw new InvalidOperationException(
                $"Postman fixture collision: survey plan scope {SurveyPlanScopeId} has incompatible ownership.");
        }

        var requestScope = await context.SurveyRequestScopes.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == SurveyRequestScopeId, cancellationToken);
        if (requestScope is not null &&
            (requestScope.SurveyRequestId != SurveyRequestId || requestScope.RouteSectionVersionId != RoadSectionVersionId ||
             requestScope.SegmentSetId != SegmentSetId || requestScope.TargetBand != "SURFACE"))
        {
            throw new InvalidOperationException(
                $"Postman fixture collision: survey request scope {SurveyRequestScopeId} has incompatible ownership.");
        }
    }

    private static bool IsDuplicateKey(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureOwnership(bool condition, Guid id, string fixtureType)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Postman fixture collision: {fixtureType} {id} has incompatible ownership or dependencies.");
        }
    }

    private static Guid RequireUser(
        Dictionary<string, RoadGuardSystem.BusinessObjects.Identity.ApplicationUser> users,
        string email)
    {
        var normalizedEmail = email.ToUpperInvariant();
        if (!users.TryGetValue(normalizedEmail, out var user))
        {
            throw new InvalidOperationException(
                $"Postman scenario fixtures require the Development account '{email}'.");
        }

        return user.Id;
    }

    private static Guid FixtureId(string suffix) =>
        Guid.Parse($"6a4dbd16-a646-46da-9e5c-20000000{suffix}");
}
