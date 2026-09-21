using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddP230FlightSurveyFileSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DroneDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    FlightNo = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.CheckConstraint("CK_Flights_TimestampOrder", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_Flights_DroneDevices_DroneDeviceId",
                        column: x => x.DroneDeviceId,
                        principalTable: "DroneDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flights_Users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileType = table.Column<byte>(type: "tinyint", nullable: false),
                    CaptureStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CaptureEndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    SyncStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    Checksum = table.Column<string>(type: "char(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyFiles", x => x.Id);
                    table.CheckConstraint("CK_SurveyFiles_Checksum_Sha256Lowercase", "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_SurveyFiles_FileType", "[FileType] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_SurveyFiles_SyncStatus", "[SyncStatus] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_SurveyFiles_TimestampOrder", "[CaptureEndedAt] IS NULL OR [CaptureStartedAt] IS NULL OR [CaptureEndedAt] >= [CaptureStartedAt]");
                    table.ForeignKey(
                        name: "FK_SurveyFiles_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyFiles_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyFiles_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_DroneDeviceId",
                table: "Flights",
                column: "DroneDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_OperatorUserId",
                table: "Flights",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Flights_SurveyFlightNo",
                table: "Flights",
                columns: new[] { "SurveyId", "FlightNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFiles_FileId",
                table: "SurveyFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFiles_FlightId",
                table: "SurveyFiles",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFiles_SurveyId",
                table: "SurveyFiles",
                column: "SurveyId");

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_SurveyFiles_ScopeIntegrity]
                ON [dbo].[SurveyFiles]
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS [surveyFile]
                        INNER JOIN [dbo].[Flights] AS [flight]
                            ON [flight].[Id] = [surveyFile].[FlightId]
                        WHERE [surveyFile].[FlightId] IS NOT NULL
                            AND [flight].[SurveyId] <> [surveyFile].[SurveyId]
                    )
                    BEGIN
                        THROW 51006, 'Survey file flight must belong to the same survey.', 1;
                    END

                    IF EXISTS
                    (
                        SELECT 1
                        FROM inserted AS [surveyFile]
                        INNER JOIN [dbo].[Files] AS [storedFile]
                            ON [storedFile].[Id] = [surveyFile].[FileId]
                        WHERE [surveyFile].[Checksum] <> [storedFile].[Checksum]
                    )
                    BEGIN
                        THROW 51007, 'Survey file checksum must match the immutable stored file checksum.', 1;
                    END
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_SurveyFiles_ScopeIntegrity];");

            migrationBuilder.DropTable(
                name: "SurveyFiles");

            migrationBuilder.DropTable(
                name: "Flights");
        }
    }
}
