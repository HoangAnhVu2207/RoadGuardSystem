using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P231ProcessingAndOutboxDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttemptCount",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "DeliveryStatus",
                table: "OutboxMessages",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorCode",
                table: "OutboxMessages",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorMessage",
                table: "OutboxMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAtUtc",
                table: "OutboxMessages",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "OutboxMessages",
                type: "varchar(120)",
                unicode: false,
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAtUtc",
                table: "OutboxMessages",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OutboxMessages",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.CreateTable(
                name: "AIModelVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelName = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    VersionLabel = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    ArtifactUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Metrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingThresholds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ReleasedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIModelVersions", x => x.Id);
                    table.CheckConstraint("CK_AIModelVersions_Metrics_Json", "[Metrics] IS NULL OR ISJSON([Metrics]) = 1");
                    table.CheckConstraint("CK_AIModelVersions_OperatingThresholds_Json", "[OperatingThresholds] IS NULL OR ISJSON([OperatingThresholds]) = 1");
                    table.CheckConstraint("CK_AIModelVersions_ReleaseMetadata", "([ReleasedAt] IS NULL AND [ReleasedByUserId] IS NULL) OR ([ReleasedAt] IS NOT NULL AND [ReleasedByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_AIModelVersions_Status", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_AIModelVersions_Users_ReleasedByUserId",
                        column: x => x.ReleasedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyDataVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockNo = table.Column<int>(type: "int", nullable: false),
                    RangeMetadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingBlocks", x => x.Id);
                    table.CheckConstraint("CK_ProcessingBlocks_BlockNo", "[BlockNo] > 0");
                    table.CheckConstraint("CK_ProcessingBlocks_RangeMetadata_JsonObject", "ISJSON([RangeMetadata]) = 1 AND LEFT(LTRIM([RangeMetadata]), 1) = '{'");
                    table.ForeignKey(
                        name: "FK_ProcessingBlocks_SurveyDataVersions_SurveyDataVersionId",
                        column: x => x.SurveyDataVersionId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingBlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingJobs", x => x.Id);
                    table.CheckConstraint("CK_ProcessingJobs_Status", "[Status] IN (1, 2, 3, 4, 5, 6)");
                    table.CheckConstraint("CK_ProcessingJobs_TimestampOrder", "[CompletedAt] IS NULL OR [StartedAt] IS NULL OR [CompletedAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_ProcessingJobs_AIModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "AIModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingJobs_ProcessingBlocks_ProcessingBlockId",
                        column: x => x.ProcessingBlockId,
                        principalTable: "ProcessingBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    ErrorType = table.Column<byte>(type: "tinyint", nullable: true),
                    WorkerReference = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingAttempts", x => x.Id);
                    table.CheckConstraint("CK_ProcessingAttempts_AttemptNo", "[AttemptNo] > 0");
                    table.CheckConstraint("CK_ProcessingAttempts_ErrorType", "[ErrorType] IS NULL OR [ErrorType] IN (1, 2, 3)");
                    table.CheckConstraint("CK_ProcessingAttempts_TimestampOrder", "[EndedAt] IS NULL OR [EndedAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_ProcessingAttempts_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DeliveryStatus_NextAttempt",
                table: "OutboxMessages",
                columns: new[] { "DeliveryStatus", "NextAttemptAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OutboxMessages_DeliveryAttemptCount",
                table: "OutboxMessages",
                sql: "[DeliveryAttemptCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OutboxMessages_DeliveryStatus",
                table: "OutboxMessages",
                sql: "[DeliveryStatus] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_AIModelVersions_ModelName",
                table: "AIModelVersions",
                column: "ModelName");

            migrationBuilder.CreateIndex(
                name: "IX_AIModelVersions_ReleasedByUserId",
                table: "AIModelVersions",
                column: "ReleasedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_AIModelVersions_VersionLabel",
                table: "AIModelVersions",
                column: "VersionLabel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingAttempts_JobId",
                table: "ProcessingAttempts",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "UX_ProcessingAttempts_JobAttemptNo",
                table: "ProcessingAttempts",
                columns: new[] { "ProcessingJobId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProcessingBlocks_DataVersionBlockNo",
                table: "ProcessingBlocks",
                columns: new[] { "SurveyDataVersionId", "BlockNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_ModelVersionId",
                table: "ProcessingJobs",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_ProcessingJobs_Block",
                table: "ProcessingJobs",
                column: "ProcessingBlockId",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_ProcessingBlocks_Immutable]
                ON [dbo].[ProcessingBlocks]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51031, 'ProcessingBlocks are immutable.', 1;
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_ProcessingAttempts_AppendOnly]
                ON [dbo].[ProcessingAttempts]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51032, 'ProcessingAttempts are append-only.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_ProcessingAttempts_AppendOnly];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_ProcessingBlocks_Immutable];");

            migrationBuilder.DropTable(
                name: "ProcessingAttempts");

            migrationBuilder.DropTable(
                name: "ProcessingJobs");

            migrationBuilder.DropTable(
                name: "AIModelVersions");

            migrationBuilder.DropTable(
                name: "ProcessingBlocks");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_DeliveryStatus_NextAttempt",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OutboxMessages_DeliveryAttemptCount",
                table: "OutboxMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OutboxMessages_DeliveryStatus",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeliveryAttemptCount",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeliveryStatus",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastErrorCode",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastErrorMessage",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OutboxMessages");
        }
    }
}
