using Microsoft.EntityFrameworkCore;
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

    public const string ProjectCode = "RG-POSTMAN-001";
    public const string RoadSectionCode = "RG-POSTMAN-RS-001";
    public const string DefectTypeCode = "RG_POSTMAN_POTHOLE";
    public const string FieldInspectionTaskCode = "RG-POSTMAN-FI-001";

    public int Order => 40;

    public string Name => nameof(PostmanScenarioSeedStep);

    public async Task SeedAsync(
        RoadGuardDbContext context,
        CancellationToken cancellationToken = default)
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
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(SegmentSetId, RoadSectionVersionId));
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
                acceptedAt: null,
                rejectedAt: null,
                rejectionReason: null,
                reassignmentReason: null,
                endedAt: null));
        }

        if (!await context.Surveys.AnyAsync(item => item.Id == SurveyId, cancellationToken))
        {
            context.Surveys.Add(Survey.Create(
                SurveyId,
                SurveyRequestId,
                ProjectId,
                RoadSectionVersionId,
                SurveyType.Periodic,
                SurveyStatus.Completed,
                isBaselineConfirmed: false,
                baselineConfirmedByUserId: null,
                baselineConfirmedAt: null));
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

        await context.SaveChangesAsync(cancellationToken);
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
