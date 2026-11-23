using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAiWorkerTransportLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiVideoWorkItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVideoFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogicalConfigurationIdentity = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    DispatchKey = table.Column<string>(type: "char(64)", unicode: false, nullable: false),
                    AdmissionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<byte>(type: "tinyint", nullable: false),
                    LeaseOwner = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    NextPollAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiVideoWorkItems", x => x.Id);
                    table.CheckConstraint("CK_AiVideoWorkItems_Admission", "ISJSON([AdmissionJson]) = 1 AND LEFT([AdmissionJson], 1) = '{'");
                    table.CheckConstraint("CK_AiVideoWorkItems_Failure", "([State] IN (6,7) AND [ErrorCode] IS NOT NULL) OR ([State] NOT IN (6,7) AND [ErrorCode] IS NULL)");
                    table.CheckConstraint("CK_AiVideoWorkItems_Lease", "([LeaseOwner] IS NULL AND [LeaseUntil] IS NULL) OR ([LeaseOwner] IS NOT NULL AND [LeaseUntil] IS NOT NULL)");
                    table.CheckConstraint("CK_AiVideoWorkItems_State", "[State] IN (1,2,3,4,5,6,7,8)");
                    table.CheckConstraint("CK_AiVideoWorkItems_TerminalLease", "[State] IN (1,2,3,4) OR [LeaseOwner] IS NULL");
                    table.CheckConstraint("CK_AiVideoWorkItems_Time", "[UpdatedAt] >= [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_AiVideoWorkItems_Files_SourceVideoFileId",
                        column: x => x.SourceVideoFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoWorkItems_ProcessingAttempts_CurrentAttemptId",
                        column: x => x.CurrentAttemptId,
                        principalTable: "ProcessingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoWorkItems_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoWorkItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoWorkItems_SurveyDataVersions_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiWorkerArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArtifactId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, nullable: false),
                    Bytes = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiWorkerArtifacts", x => x.Id);
                    table.CheckConstraint("CK_AiWorkerArtifacts_Bytes", "[Bytes] > 0");
                    table.CheckConstraint("CK_AiWorkerArtifacts_KindDimensions", "([Kind] = 'FRAME' AND [Width] BETWEEN 1 AND 16384 AND [Height] BETWEEN 1 AND 16384 AND [Width] IS NOT NULL AND [Height] IS NOT NULL) OR ([Kind] IN ('RAW_RESULT','TELEMETRY_SRT') AND [Width] IS NULL AND [Height] IS NULL)");
                    table.ForeignKey(
                        name: "FK_AiWorkerArtifacts_AiVideoWorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "AiVideoWorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiWorkerArtifacts_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiWorkerArtifacts_ProcessingAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ProcessingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoWorkItems_CurrentAttemptId",
                table: "AiVideoWorkItems",
                column: "CurrentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoWorkItems_DatasetId",
                table: "AiVideoWorkItems",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoWorkItems_Due",
                table: "AiVideoWorkItems",
                columns: new[] { "State", "NextPollAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoWorkItems_ProjectId",
                table: "AiVideoWorkItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoWorkItems_SourceVideoFileId",
                table: "AiVideoWorkItems",
                column: "SourceVideoFileId");

            migrationBuilder.CreateIndex(
                name: "UX_AiVideoWorkItems_DispatchKey",
                table: "AiVideoWorkItems",
                column: "DispatchKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AiVideoWorkItems_Job",
                table: "AiVideoWorkItems",
                column: "ProcessingJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiWorkerArtifacts_AttemptId",
                table: "AiWorkerArtifacts",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AiWorkerArtifacts_FileId",
                table: "AiWorkerArtifacts",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "UX_AiWorkerArtifacts_AttemptArtifact",
                table: "AiWorkerArtifacts",
                columns: new[] { "WorkItemId", "AttemptId", "ArtifactId" },
                unique: true);

            // Transport evidence survives retries. Only the first verified StoredFile binding is mutable.
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AiWorkerArtifacts_Immutable]
                ON [AiWorkerArtifacts] AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id = d.Id WHERE i.Id IS NULL)
                        THROW 51190, 'Worker artifact descriptors cannot be deleted.', 1;
                    IF EXISTS (
                        SELECT 1 FROM inserted i JOIN deleted d ON i.Id = d.Id
                        WHERE EXISTS (
                            SELECT i.WorkItemId, i.AttemptId, CONVERT(varbinary(max), i.ArtifactId),
                                CONVERT(varbinary(max), i.Sha256), i.Bytes,
                                CONVERT(varbinary(max), i.Kind), i.Width, i.Height
                            EXCEPT
                            SELECT d.WorkItemId, d.AttemptId, CONVERT(varbinary(max), d.ArtifactId),
                                CONVERT(varbinary(max), d.Sha256), d.Bytes,
                                CONVERT(varbinary(max), d.Kind), d.Width, d.Height)
                            OR (d.FileId IS NOT NULL AND (i.FileId IS NULL OR i.FileId <> d.FileId)))
                        THROW 51191, 'Worker artifact descriptors and registered file bindings are immutable.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiWorkerArtifacts");

            migrationBuilder.DropTable(
                name: "AiVideoWorkItems");
        }
    }
}
