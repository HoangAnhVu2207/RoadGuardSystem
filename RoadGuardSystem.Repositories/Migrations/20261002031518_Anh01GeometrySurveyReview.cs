using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RoadGuardSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class Anh01GeometrySurveyReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM RoadSegmentSets WHERE Status='PUBLISHED' GROUP BY RoadSectionVersionId HAVING COUNT(*)>1) THROW 51022, 'Audit duplicate published segment sets before applying ANH-01 geometry schema.', 1;");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM RoadSegments s JOIN RoadSegmentSets t ON s.SegmentSetId=t.Id WHERE s.RoadSectionVersionId<>t.RoadSectionVersionId) THROW 51023, 'Audit inconsistent legacy segment version references before applying ANH-01.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_RoadSegments_RoadSegmentSets_SegmentSetId",
                table: "RoadSegments");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTaskId",
                table: "SurveyRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeFormatVersion",
                table: "SurveyRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplementRequestId",
                table: "SurveyRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeFormatVersion",
                table: "SurveyPlans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Adopt only unambiguous normalized snapshots, with exact relational tuple/ID sets.
            foreach (var kind in new[] { "Plan", "Request" })
            {
                var snapshot = kind == "Plan"
                    ? "CASE WHEN ISJSON(r.OutputRequirements)=1 AND LEFT(LTRIM(r.OutputRequirements),1)='[' THEN r.OutputRequirements ELSE N'[]' END"
                    : "COALESCE(JSON_QUERY(CASE WHEN ISJSON(r.OutputRequirements)=1 THEN r.OutputRequirements ELSE N'{}' END,'$.scope'),N'[]')";
                migrationBuilder.Sql($"""
                    UPDATE r SET ScopeFormatVersion='BAND_V1'
                    FROM Survey{kind}s r
                    CROSS APPLY (SELECT {snapshot} AS body) b
                    WHERE EXISTS (SELECT 1 FROM Survey{kind}Scopes s WHERE s.Survey{kind}Id=r.Id)
                    AND (SELECT COUNT(*) FROM OPENJSON(b.body)) = (SELECT COUNT(*) FROM Survey{kind}Scopes s WHERE s.Survey{kind}Id=r.Id)
                    AND NOT EXISTS (
                      SELECT 1 FROM OPENJSON(b.body) WITH (routeVersionId nvarchar(40), segmentSetId nvarchar(40), targetBand nvarchar(20), segmentIds nvarchar(max) AS JSON) j
                      WHERE j.segmentIds IS NULL OR NOT EXISTS (SELECT 1 FROM OPENJSON(j.segmentIds))
                      OR NOT EXISTS (
                        SELECT 1 FROM Survey{kind}Scopes s WHERE s.Survey{kind}Id=r.Id
                          AND s.RouteSectionVersionId=TRY_CONVERT(uniqueidentifier,j.routeVersionId)
                          AND s.SegmentSetId=TRY_CONVERT(uniqueidentifier,j.segmentSetId) AND s.TargetBand=j.targetBand
                          AND NOT EXISTS (SELECT value FROM OPENJSON(j.segmentIds) EXCEPT SELECT value FROM OPENJSON(s.SegmentIdsJson))
                          AND NOT EXISTS (SELECT value FROM OPENJSON(s.SegmentIdsJson) EXCEPT SELECT value FROM OPENJSON(j.segmentIds))
                      )
                    );
                    """);
            }

            migrationBuilder.AddColumn<string>(
                name: "PairsManifest",
                table: "SurveyDataVersions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedBy",
                table: "SurveyDataVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefinitionJson",
                table: "RoadSegmentSets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeometryHash",
                table: "RoadSegmentSets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "RoadSegmentSets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedBy",
                table: "RoadSegmentSets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RoadSegmentSets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<double>(
                name: "EndStationMeters",
                table: "RoadSegments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FromOffsetMeters",
                table: "RoadSegments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<LineString>(
                name: "Geometry",
                table: "RoadSegments",
                type: "geometry",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StartStationMeters",
                table: "RoadSegments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ToOffsetMeters",
                table: "RoadSegments",
                type: "float",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RoadSegmentSets_Id_RoadSectionVersionId",
                table: "RoadSegmentSets",
                columns: new[] { "Id", "RoadSectionVersionId" });

            migrationBuilder.CreateTable(
                name: "BaselineSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaselineSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaselineSelections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaselineSelections_Users_SelectedBy",
                        column: x => x.SelectedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatasetAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatasetAssessments_SurveyDataVersions_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatasetAssessments_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadGeometryDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoadCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RoadName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalCoordinatesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceChecksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadGeometryDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadGeometryDrafts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadGeometryDrafts_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BaselineSelectionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaselineSelectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaselineSelectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaselineSelectionItems_BaselineSelections_BaselineSelectionId",
                        column: x => x.BaselineSelectionId,
                        principalTable: "BaselineSelections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaselineSelectionItems_DatasetAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "DatasetAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaselineSelectionItems_SurveyDataVersions_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatasetAssessmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PositionStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    QualityStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CoverageStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasetAssessmentItems", x => x.Id);
                    table.CheckConstraint("CK_Assessment_Status", "[PositionStatus] IN ('PASS','FAIL','UNKNOWN') AND [QualityStatus] IN ('PASS','FAIL','UNKNOWN') AND [CoverageStatus] IN ('PASS','FAIL','UNKNOWN')");
                    table.ForeignKey(
                        name: "FK_DatasetAssessmentItems_DatasetAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "DatasetAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatasetAssessmentItems_RoadSegments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadGeometryMetadata",
                columns: table => new
                {
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeometryHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadGeometryMetadata", x => x.RoadSectionVersionId);
                    table.ForeignKey(
                        name: "FK_RoadGeometryMetadata_RoadGeometryDrafts_SourceDraftId",
                        column: x => x.SourceDraftId,
                        principalTable: "RoadGeometryDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadGeometryMetadata_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BaselineCurrentPointers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetBand = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SelectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaselineCurrentPointers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaselineCurrentPointers_BaselineSelectionItems_SelectionId",
                        column: x => x.SelectionId,
                        principalTable: "BaselineSelectionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaselineCurrentPointers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_ParentTaskId",
                table: "SurveyRequests",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyRequests_SupplementRequestId",
                table: "SurveyRequests",
                column: "SupplementRequestId");

            migrationBuilder.CreateIndex(
                name: "UX_RoadSegmentSets_CurrentPublished",
                table: "RoadSegmentSets",
                column: "RoadSectionVersionId",
                unique: true,
                filter: "[Status] = 'PUBLISHED'");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_SegmentSetId_RoadSectionVersionId",
                table: "RoadSegments",
                columns: new[] { "SegmentSetId", "RoadSectionVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_BaselineCurrentPointers_ProjectId_RouteVersionId_SegmentSetId_SegmentId_TargetBand",
                table: "BaselineCurrentPointers",
                columns: new[] { "ProjectId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaselineCurrentPointers_SelectionId",
                table: "BaselineCurrentPointers",
                column: "SelectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelectionItems_AssessmentId",
                table: "BaselineSelectionItems",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelectionItems_BaselineSelectionId_RouteVersionId_SegmentSetId_SegmentId_TargetBand",
                table: "BaselineSelectionItems",
                columns: new[] { "BaselineSelectionId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelectionItems_DatasetId",
                table: "BaselineSelectionItems",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelections_ProjectId",
                table: "BaselineSelections",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BaselineSelections_SelectedBy",
                table: "BaselineSelections",
                column: "SelectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessmentItems_AssessmentId_RouteVersionId_SegmentSetId_SegmentId_TargetBand",
                table: "DatasetAssessmentItems",
                columns: new[] { "AssessmentId", "RouteVersionId", "SegmentSetId", "SegmentId", "TargetBand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessmentItems_SegmentId",
                table: "DatasetAssessmentItems",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessments_DatasetId_ReviewedAt",
                table: "DatasetAssessments",
                columns: new[] { "DatasetId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DatasetAssessments_ReviewedBy",
                table: "DatasetAssessments",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoadGeometryDrafts_ProjectId",
                table: "RoadGeometryDrafts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadGeometryDrafts_RoadSectionId",
                table: "RoadGeometryDrafts",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadGeometryMetadata_SourceDraftId",
                table: "RoadGeometryMetadata",
                column: "SourceDraftId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoadSegments_RoadSegmentSets_SegmentSetId_RoadSectionVersionId",
                table: "RoadSegments",
                columns: new[] { "SegmentSetId", "RoadSectionVersionId" },
                principalTable: "RoadSegmentSets",
                principalColumns: new[] { "Id", "RoadSectionVersionId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyRequests_SupplementarySurveyRequests_SupplementRequestId",
                table: "SurveyRequests",
                column: "SupplementRequestId",
                principalTable: "SupplementarySurveyRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyRequests_SurveyRequests_ParentTaskId",
                table: "SurveyRequests",
                column: "ParentTaskId",
                principalTable: "SurveyRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            foreach (var table in new[] { "DatasetAssessments", "DatasetAssessmentItems", "BaselineSelections", "BaselineSelectionItems", "RoadGeometryMetadata" })
                migrationBuilder.Sql($"CREATE TRIGGER [TR_{table}_Immutable] ON [{table}] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51024, 'ANH-01 evidence and geometry metadata are immutable.', 1; END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM DatasetAssessments) OR EXISTS (SELECT 1 FROM BaselineSelections) OR EXISTS (SELECT 1 FROM RoadGeometryMetadata) THROW 51025, 'ANH-01 downgrade requires an owner-reviewed preservation plan for immutable evidence.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_RoadSegments_RoadSegmentSets_SegmentSetId_RoadSectionVersionId",
                table: "RoadSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyRequests_SupplementarySurveyRequests_SupplementRequestId",
                table: "SurveyRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SurveyRequests_SurveyRequests_ParentTaskId",
                table: "SurveyRequests");

            migrationBuilder.DropTable(
                name: "BaselineCurrentPointers");

            migrationBuilder.DropTable(
                name: "DatasetAssessmentItems");

            migrationBuilder.DropTable(
                name: "RoadGeometryMetadata");

            migrationBuilder.DropTable(
                name: "BaselineSelectionItems");

            migrationBuilder.DropTable(
                name: "RoadGeometryDrafts");

            migrationBuilder.DropTable(
                name: "BaselineSelections");

            migrationBuilder.DropTable(
                name: "DatasetAssessments");

            migrationBuilder.DropIndex(
                name: "IX_SurveyRequests_ParentTaskId",
                table: "SurveyRequests");

            migrationBuilder.DropIndex(
                name: "IX_SurveyRequests_SupplementRequestId",
                table: "SurveyRequests");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RoadSegmentSets_Id_RoadSectionVersionId",
                table: "RoadSegmentSets");

            migrationBuilder.DropIndex(
                name: "UX_RoadSegmentSets_CurrentPublished",
                table: "RoadSegmentSets");

            migrationBuilder.DropIndex(
                name: "IX_RoadSegments_SegmentSetId_RoadSectionVersionId",
                table: "RoadSegments");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "SurveyRequests");

            migrationBuilder.DropColumn(
                name: "ScopeFormatVersion",
                table: "SurveyRequests");

            migrationBuilder.DropColumn(
                name: "SupplementRequestId",
                table: "SurveyRequests");

            migrationBuilder.DropColumn(
                name: "ScopeFormatVersion",
                table: "SurveyPlans");

            migrationBuilder.DropColumn(
                name: "PairsManifest",
                table: "SurveyDataVersions");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                table: "SurveyDataVersions");

            migrationBuilder.DropColumn(
                name: "DefinitionJson",
                table: "RoadSegmentSets");

            migrationBuilder.DropColumn(
                name: "GeometryHash",
                table: "RoadSegmentSets");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "RoadSegmentSets");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                table: "RoadSegmentSets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RoadSegmentSets");

            migrationBuilder.DropColumn(
                name: "EndStationMeters",
                table: "RoadSegments");

            migrationBuilder.DropColumn(
                name: "FromOffsetMeters",
                table: "RoadSegments");

            migrationBuilder.DropColumn(
                name: "Geometry",
                table: "RoadSegments");

            migrationBuilder.DropColumn(
                name: "StartStationMeters",
                table: "RoadSegments");

            migrationBuilder.DropColumn(
                name: "ToOffsetMeters",
                table: "RoadSegments");

            migrationBuilder.AddForeignKey(
                name: "FK_RoadSegments_RoadSegmentSets_SegmentSetId",
                table: "RoadSegments",
                column: "SegmentSetId",
                principalTable: "RoadSegmentSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
