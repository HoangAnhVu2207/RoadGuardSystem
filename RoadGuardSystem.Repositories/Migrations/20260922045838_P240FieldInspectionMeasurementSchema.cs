using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P240FieldInspectionMeasurementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FieldInspectionAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldInspectionTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionAssignments", x => x.Id);
                    table.CheckConstraint("CK_FieldInspectionAssignments_State", "([Status] = 1 AND [EndedAt] IS NULL AND [Reason] IS NULL) OR ([Status] IN (2, 3) AND [EndedAt] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason]))) > 0)");
                    table.CheckConstraint("CK_FieldInspectionAssignments_Status", "[Status] IN (1, 2, 3)");
                    table.CheckConstraint("CK_FieldInspectionAssignments_TimestampOrder", "[EndedAt] IS NULL OR [EndedAt] >= [AssignedAt]");
                    table.ForeignKey(
                        name: "FK_FieldInspectionAssignments_FieldInspectionTasks_FieldInspectionTaskId",
                        column: x => x.FieldInspectionTaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionAssignments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionAssignments_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldInspectionSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    FieldInspectionTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionCode = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    InspectorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectorName = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    ConductedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    WeatherCondition = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Method = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    EvidenceFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldInspectionSessions", x => x.Id);
                    table.CheckConstraint("CK_FieldInspectionSessions_Purpose", "[Purpose] IN (1, 2)");
                    table.CheckConstraint("CK_FieldInspectionSessions_PurposeScope", "([Purpose] = 1 AND [FieldInspectionTaskId] IS NOT NULL AND [SurveyId] IS NOT NULL AND [InspectorUserId] IS NOT NULL) OR ([Purpose] = 2 AND [FieldInspectionTaskId] IS NULL)");
                    table.CheckConstraint("CK_FieldInspectionSessions_Status", "[Status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_FieldInspectionTasks_FieldInspectionTaskId",
                        column: x => x.FieldInspectionTaskId,
                        principalTable: "FieldInspectionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_Files_EvidenceFileId",
                        column: x => x.EvidenceFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FieldInspectionSessions_Users_InspectorUserId",
                        column: x => x.InspectorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroundTruthMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldInspectionSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    RoadSectionVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MeasurementType = table.Column<byte>(type: "tinyint", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(19,6)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: false),
                    InstrumentName = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    InstrumentReference = table.Column<string>(type: "nvarchar(150)", nullable: true),
                    MeasurementMethod = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    MeasuredBy = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EvidenceFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroundTruthMeasurements", x => x.Id);
                    table.CheckConstraint("CK_GroundTruthMeasurements_EvidenceOrReason", "[EvidenceFileId] IS NOT NULL OR LEN(LTRIM(RTRIM([Notes]))) > 0");
                    table.CheckConstraint("CK_GroundTruthMeasurements_Location", "[Location].STSrid = 4326 AND [Location].STIsEmpty() = 0");
                    table.CheckConstraint("CK_GroundTruthMeasurements_MeasurementType", "[MeasurementType] IN (1, 2, 3)");
                    table.CheckConstraint("CK_GroundTruthMeasurements_Unit", "LOWER([Unit]) IN ('mm', 'cm', 'm')");
                    table.CheckConstraint("CK_GroundTruthMeasurements_Value", "[Value] >= 0");
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_FieldInspectionSessions_FieldInspectionSessionId",
                        column: x => x.FieldInspectionSessionId,
                        principalTable: "FieldInspectionSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_Files_EvidenceFileId",
                        column: x => x.EvidenceFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_RoadSectionVersions_RoadSectionVersionId",
                        column: x => x.RoadSectionVersionId,
                        principalTable: "RoadSectionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroundTruthMeasurements_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionAssignments_AssignedByUserId",
                table: "FieldInspectionAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionAssignments_AssignedToUserId",
                table: "FieldInspectionAssignments",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionAssignments_ActiveTask",
                table: "FieldInspectionAssignments",
                column: "FieldInspectionTaskId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_EvidenceFileId",
                table: "FieldInspectionSessions",
                column: "EvidenceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_FieldInspectionTaskId",
                table: "FieldInspectionSessions",
                column: "FieldInspectionTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_InspectorUserId",
                table: "FieldInspectionSessions",
                column: "InspectorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_ProjectVersion",
                table: "FieldInspectionSessions",
                columns: new[] { "ProjectId", "RoadSectionVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_RoadSectionVersionId",
                table: "FieldInspectionSessions",
                column: "RoadSectionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldInspectionSessions_SurveyId",
                table: "FieldInspectionSessions",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "UX_FieldInspectionSessions_SessionCode",
                table: "FieldInspectionSessions",
                column: "SessionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_DefectId",
                table: "GroundTruthMeasurements",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_EvidenceFileId",
                table: "GroundTruthMeasurements",
                column: "EvidenceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_RoadVersionType",
                table: "GroundTruthMeasurements",
                columns: new[] { "RoadSectionVersionId", "MeasurementType" });

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruthMeasurements_SurveyId",
                table: "GroundTruthMeasurements",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "UX_GroundTruthMeasurements_SessionSample",
                table: "GroundTruthMeasurements",
                columns: new[] { "FieldInspectionSessionId", "SampleId" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_FieldInspectionSessions_Integrity]
                ON [dbo].[FieldInspectionSessions]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS session
                        LEFT JOIN [dbo].[FieldInspectionTasks] AS task
                            ON task.[Id] = session.[FieldInspectionTaskId]
                        WHERE (session.[Purpose] = 1 AND
                               (task.[Id] IS NULL OR
                                task.[ProjectId] <> session.[ProjectId] OR
                                task.[RoadSectionVersionId] <> session.[RoadSectionVersionId] OR
                                task.[SurveyId] <> session.[SurveyId] OR
                                NOT EXISTS
                                (
                                    SELECT 1
                                    FROM [dbo].[FieldInspectionAssignments] AS assignment
                                    WHERE assignment.[FieldInspectionTaskId] = task.[Id]
                                      AND assignment.[AssignedToUserId] = session.[InspectorUserId]
                                      AND assignment.[Status] = 1
                                )))
                           OR (session.[Purpose] = 2 AND session.[FieldInspectionTaskId] IS NOT NULL)
                    )
                    BEGIN
                        THROW 51040, 'Field inspection session scope or active assignment is invalid.', 1;
                    END
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_FieldInspectionSessions_Immutable]
                ON [dbo].[FieldInspectionSessions]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS
                    (
                        SELECT 1
                        FROM deleted AS old_session
                        WHERE old_session.[Status] IN (2, 3, 4)
                    )
                    BEGIN
                        THROW 51041, 'Completed, imported, or locked field inspection sessions are immutable.', 1;
                    END
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_GroundTruthMeasurements_Integrity]
                ON [dbo].[GroundTruthMeasurements]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS measurement
                        INNER JOIN [dbo].[FieldInspectionSessions] AS session
                            ON session.[Id] = measurement.[FieldInspectionSessionId]
                        LEFT JOIN [dbo].[FieldInspectionTasks] AS task
                            ON task.[Id] = session.[FieldInspectionTaskId]
                        LEFT JOIN [dbo].[Defects] AS defect
                            ON defect.[Id] = measurement.[DefectId]
                        WHERE measurement.[RoadSectionVersionId] <> session.[RoadSectionVersionId]
                           OR (session.[Purpose] = 1 AND
                               (measurement.[DefectId] IS NULL OR
                                measurement.[SurveyId] IS NULL OR
                                measurement.[SurveyId] <> session.[SurveyId] OR
                                measurement.[DefectId] <> task.[DefectId] OR
                                defect.[ProjectId] <> session.[ProjectId] OR
                                defect.[RoadSectionVersionId] <> session.[RoadSectionVersionId] OR
                                defect.[Status] <> 1))
                           OR (session.[Purpose] = 2 AND
                               (measurement.[DefectId] IS NOT NULL OR measurement.[SurveyId] IS NOT NULL))
                    )
                    BEGIN
                        THROW 51042, 'Ground truth measurement purpose or scope is invalid.', 1;
                    END
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_GroundTruthMeasurements_Immutable]
                ON [dbo].[GroundTruthMeasurements]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS
                    (
                        SELECT 1
                        FROM deleted AS old_measurement
                        INNER JOIN [dbo].[FieldInspectionSessions] AS session
                            ON session.[Id] = old_measurement.[FieldInspectionSessionId]
                        WHERE session.[Status] IN (2, 3, 4)
                    )
                    BEGIN
                        THROW 51043, 'Submitted ground truth measurements are immutable.', 1;
                    END
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_GroundTruthMeasurements_Immutable];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_GroundTruthMeasurements_Integrity];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_FieldInspectionSessions_Immutable];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_FieldInspectionSessions_Integrity];");

            migrationBuilder.DropTable(
                name: "FieldInspectionAssignments");

            migrationBuilder.DropTable(
                name: "GroundTruthMeasurements");

            migrationBuilder.DropTable(
                name: "FieldInspectionSessions");
        }
    }
}
