using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAiVideoAttemptOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiVideoAttemptOutcomes",
                columns: table => new
                {
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<byte>(type: "tinyint", nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    AdmissionHash = table.Column<string>(type: "char(64)", unicode: false, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiVideoAttemptOutcomes", x => x.AttemptId);
                    table.CheckConstraint("CK_AiVideoAttemptOutcomes_AdmissionHash", "LEN([AdmissionHash])=64 AND [AdmissionHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_AiVideoAttemptOutcomes_StateError", "([State] IN (5,8) AND [ErrorCode] IS NULL) OR ([State] IN (6,7) AND [ErrorCode] IS NOT NULL AND LEN([ErrorCode]) BETWEEN 1 AND 80 AND [ErrorCode] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z0-9_]%')");
                    table.ForeignKey(
                        name: "FK_AiVideoAttemptOutcomes_AiVideoWorkItems_WorkId",
                        column: x => x.WorkId,
                        principalTable: "AiVideoWorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoAttemptOutcomes_ProcessingAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ProcessingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoAttemptOutcomes_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoAttemptOutcomes_ProcessingJobId",
                table: "AiVideoAttemptOutcomes",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoAttemptOutcomes_WorkId_OccurredAt",
                table: "AiVideoAttemptOutcomes",
                columns: new[] { "WorkId", "OccurredAt" });

            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AiVideoAttemptOutcomes_AppendOnly] ON [AiVideoAttemptOutcomes]
                INSTEAD OF UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'AI_ATTEMPT_OUTCOME_APPEND_ONLY', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AiVideoAttemptOutcomes");
        }
    }
}
