using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AnhHuySharedIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "FileScopes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "IncidentCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerificationMethod = table.Column<int>(type: "int", nullable: true),
                    TriageReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TriagedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LinkedTargetCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeometryRouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GeometrySegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    ActiveReportIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentCases", x => x.Id);
                    table.CheckConstraint("CK_IncidentCases_GeometryPair", "([GeometryRouteVersionId] IS NULL AND [GeometrySegmentSetId] IS NULL) OR ([GeometryRouteVersionId] IS NOT NULL AND [GeometrySegmentSetId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_IncidentCases_IncidentCases_LinkedTargetCaseId",
                        column: x => x.LinkedTargetCaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentCases_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentCases_RoadSegmentSets_GeometrySegmentSetId_GeometryRouteVersionId",
                        columns: x => new { x.GeometrySegmentSetId, x.GeometryRouteVersionId },
                        principalTable: "RoadSegmentSets",
                        principalColumns: new[] { "Id", "RoadSectionVersionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.UniqueConstraint("AK_Reports_Id_ReporterUserId", x => new { x.Id, x.ReporterUserId });
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseConclusions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ConcludedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseConclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseConclusions_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DefectIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientReportIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CasePublications_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportLinkHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReportIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportLinkHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistory_IncidentCases_FromCaseId",
                        column: x => x.FromCaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistory_IncidentCases_ToCaseId",
                        column: x => x.ToCaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistory_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportLinks", x => x.Id);
                    table.CheckConstraint("CK_CaseReportLinks_Times", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_CaseReportLinks_IncidentCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "IncidentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinks_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportOriginalEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationState = table.Column<int>(type: "int", nullable: false),
                    CaptureMetadata_CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CaptureMetadata_Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_AccuracyMeters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    CaptureMetadata_LocationSource = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportOriginalEvidence", x => x.Id);
                    table.CheckConstraint("CK_ReportOriginalEvidence_Original", "[SupplementId] IS NULL AND [VerificationState] = 1");
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Reports_ReportId_OwnerUserId",
                        columns: x => new { x.ReportId, x.OwnerUserId },
                        principalTable: "Reports",
                        principalColumns: new[] { "Id", "ReporterUserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportOriginalEvidence_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportSupplements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSupplements", x => x.Id);
                    table.UniqueConstraint("AK_ReportSupplements_Id_ReportId", x => new { x.Id, x.ReportId });
                    table.ForeignKey(
                        name: "FK_ReportSupplements_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourceDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source_Kind = table.Column<int>(type: "int", nullable: false),
                    Source_Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source_Version = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeometryVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Classification_RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification_SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification_DefectTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Classification_CauseCategoryCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Classification_Severity = table.Column<byte>(type: "tinyint", nullable: true),
                    TargetDefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetDefectVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupersedesDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpectedPreviousDecisionVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AIDetectionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceDecisions", x => x.Id);
                    table.UniqueConstraint("AK_SourceDecisions_Id_SourceKind_SourceId_ProjectId", x => new { x.Id, x.SourceKind, x.SourceId, x.ProjectId });
                    table.CheckConstraint("CK_SourceDecisions_TypedSource", "[SourceKind] = [Source_Kind] AND [SourceId] = [Source_Id] AND (([SourceKind]=1 AND [ReportSourceId]=[SourceId] AND [ReportSourceId] IS NOT NULL AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]=2 AND [AIDetectionSourceId]=[SourceId] AND [AIDetectionSourceId] IS NOT NULL AND [ReportSourceId] IS NULL))");
                    table.ForeignKey(
                        name: "FK_SourceDecisions_AIDetections_AIDetectionSourceId",
                        column: x => x.AIDetectionSourceId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_CauseCategories_Classification_CauseCategoryCode",
                        column: x => x.Classification_CauseCategoryCode,
                        principalTable: "CauseCategories",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_DefectTypes_Classification_DefectTypeCode",
                        column: x => x.Classification_DefectTypeCode,
                        principalTable: "DefectTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Defects_TargetDefectId",
                        column: x => x.TargetDefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Reports_ReportSourceId",
                        column: x => x.ReportSourceId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_RoadSectionVersions_Classification_RoadSectionVersionId",
                        column: x => x.Classification_RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_RoadSegments_Classification_SegmentId",
                        column: x => x.Classification_SegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_SourceDecisions_SupersedesDecisionId",
                        column: x => x.SupersedesDecisionId,
                        principalTable: "SourceDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourceDecisions_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseConclusionDefects",
                columns: table => new
                {
                    ConclusionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseConclusionDefects", x => new { x.ConclusionId, x.DefectId });
                    table.ForeignKey(
                        name: "FK_CaseConclusionDefects_CaseConclusions_ConclusionId",
                        column: x => x.ConclusionId,
                        principalTable: "CaseConclusions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseConclusionDefects_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseConclusionEvidence",
                columns: table => new
                {
                    ConclusionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplementEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseConclusionEvidence", x => new { x.ConclusionId, x.EvidenceId });
                    table.CheckConstraint("CK_CaseConclusionEvidence_TypedEvidence", "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_CaseConclusionEvidence_CaseConclusions_ConclusionId",
                        column: x => x.ConclusionId,
                        principalTable: "CaseConclusions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseConclusionEvidence_Reports_SourceReportId",
                        column: x => x.SourceReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublicationDefects",
                columns: table => new
                {
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublicationDefects", x => new { x.PublicationId, x.DefectId });
                    table.ForeignKey(
                        name: "FK_CasePublicationDefects_CasePublications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "CasePublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CasePublicationDefects_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublicationRecipients",
                columns: table => new
                {
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublicationRecipients", x => new { x.PublicationId, x.ReportId });
                    table.ForeignKey(
                        name: "FK_CasePublicationRecipients_CasePublications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "CasePublications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CasePublicationRecipients_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportLinkHistoryReports",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportLinkHistoryReports", x => new { x.HistoryId, x.ReportId });
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistoryReports_CaseReportLinkHistory_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "CaseReportLinkHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportLinkHistoryReports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportSupplementEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationState = table.Column<int>(type: "int", nullable: false),
                    CaptureMetadata_CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CaptureMetadata_Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CaptureMetadata_AccuracyMeters = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    CaptureMetadata_LocationSource = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSupplementEvidence", x => x.Id);
                    table.CheckConstraint("CK_ReportSupplementEvidence_Supplement", "[SupplementId] IS NOT NULL AND [VerificationState] = 1");
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_ReportSupplements_SupplementId_ReportId",
                        columns: x => new { x.SupplementId, x.ReportId },
                        principalTable: "ReportSupplements",
                        principalColumns: new[] { "Id", "ReportId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_Reports_ReportId_OwnerUserId",
                        columns: x => new { x.ReportId, x.OwnerUserId },
                        principalTable: "Reports",
                        principalColumns: new[] { "Id", "ReporterUserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportSupplementEvidence_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CandidateSourceHeads",
                columns: table => new
                {
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateSourceHeads", x => new { x.SourceKind, x.SourceId });
                    table.ForeignKey(
                        name: "FK_CandidateSourceHeads_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CandidateSourceHeads_SourceDecisions_DecisionId_SourceKind_SourceId_ProjectId",
                        columns: x => new { x.DecisionId, x.SourceKind, x.SourceId, x.ProjectId },
                        principalTable: "SourceDecisions",
                        principalColumns: new[] { "Id", "SourceKind", "SourceId", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CasePublicationEvidence",
                columns: table => new
                {
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplementEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePublicationEvidence", x => new { x.PublicationId, x.RecipientReportId, x.EvidenceId });
                    table.CheckConstraint("CK_CasePublicationEvidence_TypedEvidence", "([OriginalEvidenceId] IS NOT NULL AND [OriginalEvidenceId]=[EvidenceId] AND [SupplementEvidenceId] IS NULL) OR ([SupplementEvidenceId] IS NOT NULL AND [SupplementEvidenceId]=[EvidenceId] AND [OriginalEvidenceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_CasePublicationEvidence_CasePublicationRecipients_PublicationId_RecipientReportId",
                        columns: x => new { x.PublicationId, x.RecipientReportId },
                        principalTable: "CasePublicationRecipients",
                        principalColumns: new[] { "PublicationId", "ReportId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CasePublicationEvidence_Reports_SourceReportId",
                        column: x => x.SourceReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_FileScopes_PrivateShape",
                table: "FileScopes",
                sql: "[ProjectId] IS NOT NULL OR ([Purpose] = 'REPORT_PHOTO' AND [TargetId] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSourceHeads_DecisionId_SourceKind_SourceId_ProjectId",
                table: "CandidateSourceHeads",
                columns: new[] { "DecisionId", "SourceKind", "SourceId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSourceHeads_ProjectId",
                table: "CandidateSourceHeads",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConclusionDefects_DefectId",
                table: "CaseConclusionDefects",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConclusionEvidence_SourceReportId",
                table: "CaseConclusionEvidence",
                column: "SourceReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseConclusions_CaseId",
                table: "CaseConclusions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublicationDefects_DefectId",
                table: "CasePublicationDefects",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublicationEvidence_SourceReportId",
                table: "CasePublicationEvidence",
                column: "SourceReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublicationRecipients_ReportId",
                table: "CasePublicationRecipients",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CasePublications_CaseId",
                table: "CasePublications",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistory_ActorUserId",
                table: "CaseReportLinkHistory",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistory_FromCaseId",
                table: "CaseReportLinkHistory",
                column: "FromCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistory_ToCaseId",
                table: "CaseReportLinkHistory",
                column: "ToCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinkHistoryReports_ReportId",
                table: "CaseReportLinkHistoryReports",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinks_CaseId_ReportId",
                table: "CaseReportLinks",
                columns: new[] { "CaseId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportLinks_ReportId",
                table: "CaseReportLinks",
                column: "ReportId",
                unique: true,
                filter: "[EndedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCases_GeometrySegmentSetId_GeometryRouteVersionId",
                table: "IncidentCases",
                columns: new[] { "GeometrySegmentSetId", "GeometryRouteVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCases_LinkedTargetCaseId",
                table: "IncidentCases",
                column: "LinkedTargetCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCases_ProjectId",
                table: "IncidentCases",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_FileId",
                table: "ReportOriginalEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_Id_ReportId",
                table: "ReportOriginalEvidence",
                columns: new[] { "Id", "ReportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_OwnerUserId",
                table: "ReportOriginalEvidence",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportOriginalEvidence_ReportId_OwnerUserId",
                table: "ReportOriginalEvidence",
                columns: new[] { "ReportId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterUserId",
                table: "Reports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_FileId",
                table: "ReportSupplementEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_Id_ReportId",
                table: "ReportSupplementEvidence",
                columns: new[] { "Id", "ReportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_OwnerUserId",
                table: "ReportSupplementEvidence",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_ReportId_OwnerUserId",
                table: "ReportSupplementEvidence",
                columns: new[] { "ReportId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplementEvidence_SupplementId_ReportId",
                table: "ReportSupplementEvidence",
                columns: new[] { "SupplementId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSupplements_ReportId",
                table: "ReportSupplements",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_AIDetectionSourceId",
                table: "SourceDecisions",
                column: "AIDetectionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_CauseCategoryCode",
                table: "SourceDecisions",
                column: "Classification_CauseCategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_DefectTypeCode",
                table: "SourceDecisions",
                column: "Classification_DefectTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_RoadSectionVersionId",
                table: "SourceDecisions",
                column: "Classification_RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_Classification_SegmentId",
                table: "SourceDecisions",
                column: "Classification_SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_DecidedByUserId",
                table: "SourceDecisions",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_ProjectId",
                table: "SourceDecisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_ReportSourceId",
                table: "SourceDecisions",
                column: "ReportSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_SupersedesDecisionId",
                table: "SourceDecisions",
                column: "SupersedesDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceDecisions_TargetDefectId",
                table: "SourceDecisions",
                column: "TargetDefectId");

            migrationBuilder.Sql("ALTER TABLE [CasePublicationEvidence] ADD CONSTRAINT [FK_CasePublicationEvidence_OriginalEvidence] FOREIGN KEY ([OriginalEvidenceId],[SourceReportId]) REFERENCES [ReportOriginalEvidence] ([Id],[ReportId]);");
            migrationBuilder.Sql("ALTER TABLE [CasePublicationEvidence] ADD CONSTRAINT [FK_CasePublicationEvidence_SupplementEvidence] FOREIGN KEY ([SupplementEvidenceId],[SourceReportId]) REFERENCES [ReportSupplementEvidence] ([Id],[ReportId]);");
            migrationBuilder.Sql("ALTER TABLE [CaseConclusionEvidence] ADD CONSTRAINT [FK_CaseConclusionEvidence_OriginalEvidence] FOREIGN KEY ([OriginalEvidenceId],[SourceReportId]) REFERENCES [ReportOriginalEvidence] ([Id],[ReportId]);");
            migrationBuilder.Sql("ALTER TABLE [CaseConclusionEvidence] ADD CONSTRAINT [FK_CaseConclusionEvidence_SupplementEvidence] FOREIGN KEY ([SupplementEvidenceId],[SourceReportId]) REFERENCES [ReportSupplementEvidence] ([Id],[ReportId]);");
            migrationBuilder.Sql("CREATE TRIGGER [TR_ReportOriginalEvidence_Immutable] ON [ReportOriginalEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51100, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_ReportSupplementEvidence_Immutable] ON [ReportSupplementEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51101, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_ReportSupplements_Immutable] ON [ReportSupplements] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51102, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CaseConclusions_Immutable] ON [CaseConclusions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51103, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CasePublications_Immutable] ON [CasePublications] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51104, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CasePublicationRecipients_Immutable] ON [CasePublicationRecipients] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51105, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CasePublicationEvidence_Immutable] ON [CasePublicationEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51106, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CaseReportLinkHistory_Immutable] ON [CaseReportLinkHistory] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51107, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_SourceDecisions_Immutable] ON [SourceDecisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51108, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CaseConclusionDefects_Immutable] ON [CaseConclusionDefects] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51109, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CasePublicationDefects_Immutable] ON [CasePublicationDefects] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51110, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CaseConclusionEvidence_Immutable] ON [CaseConclusionEvidence] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51111, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CaseReportLinkHistoryReports_Immutable] ON [CaseReportLinkHistoryReports] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51112, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_FileScopes_Immutable] ON [FileScopes] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51113, 'Immutable integration history cannot be rewritten.', 1; END;");
            migrationBuilder.Sql("ALTER TABLE [SourceDecisions] ADD CONSTRAINT [FK_SourceDecisions_CorrectionIdentity] FOREIGN KEY ([SupersedesDecisionId],[SourceKind],[SourceId],[ProjectId]) REFERENCES [SourceDecisions] ([Id],[SourceKind],[SourceId],[ProjectId]);");
            migrationBuilder.Sql("CREATE TRIGGER [TR_CaseReportLinks_AppendOnly] ON [CaseReportLinks] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT Id,CaseId,ReportId,StartedAt FROM deleted EXCEPT SELECT Id,CaseId,ReportId,StartedAt FROM inserted) OR EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id WHERE d.EndedAt IS NOT NULL AND (i.EndedAt IS NULL OR i.EndedAt<>d.EndedAt)) THROW 51122, 'Link identity/history cannot be rewritten or reopened.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_Reports_OriginalImmutable] ON [Reports] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM deleted EXCEPT SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM inserted) OR EXISTS (SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM inserted EXCEPT SELECT Id,ReporterUserId,Description COLLATE Latin1_General_100_BIN2,DATALENGTH(Description),ReceivedAt FROM deleted) THROW 51120, 'Original report is immutable.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_ReportOriginalEvidence_Identity] ON [ReportOriginalEvidence] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM inserted i JOIN [ReportSupplementEvidence] e WITH (UPDLOCK,HOLDLOCK) ON e.Id=i.Id) THROW 51121, 'Evidence identity already exists in another evidence namespace.', 1; END;");
            migrationBuilder.Sql("CREATE TRIGGER [TR_ReportSupplementEvidence_Identity] ON [ReportSupplementEvidence] AFTER INSERT AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM inserted i JOIN [ReportOriginalEvidence] e WITH (UPDLOCK,HOLDLOCK) ON e.Id=i.Id) THROW 51121, 'Evidence identity already exists in another evidence namespace.', 1; END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recovery is additive. Never discard received Reports or private evidence.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM Reports) OR EXISTS (SELECT 1 FROM IncidentCases) OR EXISTS (SELECT 1 FROM SourceDecisions) OR EXISTS (SELECT 1 FROM FileScopes WHERE ProjectId IS NULL) THROW 51130, 'Integration data exists; rollback requires an owner-approved recovery plan.', 1;");
            migrationBuilder.Sql("DROP TRIGGER [TR_FileScopes_Immutable];");

            migrationBuilder.DropTable(
                name: "CandidateSourceHeads");

            migrationBuilder.DropTable(
                name: "CaseConclusionDefects");

            migrationBuilder.DropTable(
                name: "CaseConclusionEvidence");

            migrationBuilder.DropTable(
                name: "CasePublicationDefects");

            migrationBuilder.DropTable(
                name: "CasePublicationEvidence");

            migrationBuilder.DropTable(
                name: "CaseReportLinkHistoryReports");

            migrationBuilder.DropTable(
                name: "CaseReportLinks");

            migrationBuilder.DropTable(
                name: "ReportOriginalEvidence");

            migrationBuilder.DropTable(
                name: "ReportSupplementEvidence");

            migrationBuilder.DropTable(
                name: "SourceDecisions");

            migrationBuilder.DropTable(
                name: "CaseConclusions");

            migrationBuilder.DropTable(
                name: "CasePublicationRecipients");

            migrationBuilder.DropTable(
                name: "CaseReportLinkHistory");

            migrationBuilder.DropTable(
                name: "ReportSupplements");

            migrationBuilder.DropTable(
                name: "CasePublications");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "IncidentCases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FileScopes_PrivateShape",
                table: "FileScopes");

            migrationBuilder.DropForeignKey("FK_FileScopes_Projects_ProjectId", "FileScopes");
            migrationBuilder.DropIndex("IX_FileScopes_ProjectId_OwnerUserId", "FileScopes");
            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "FileScopes",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
            migrationBuilder.CreateIndex("IX_FileScopes_ProjectId_OwnerUserId", "FileScopes", new[] { "ProjectId", "OwnerUserId" });
            migrationBuilder.AddForeignKey("FK_FileScopes_Projects_ProjectId", "FileScopes", "ProjectId", "Projects", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}
