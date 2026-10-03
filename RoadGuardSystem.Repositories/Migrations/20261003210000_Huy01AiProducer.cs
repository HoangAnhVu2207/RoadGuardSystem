using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class Huy01AiProducer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anh02AiDetectionProvenance",
                columns: table => new
                {
                    DetectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVideoFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVideoFileVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    FrameFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameFileVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    DerivationHash = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    TimestampMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    SourceDurationMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    SegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BoxX = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false),
                    BoxY = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false),
                    BoxWidth = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false),
                    BoxHeight = table.Column<decimal>(type: "decimal(12,9)", precision: 12, scale: 9, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiDetectionProvenance", x => x.DetectionId);
                    table.CheckConstraint("CK_Anh02AiDetection_Box", "[BoxX]>=0 AND [BoxY]>=0 AND [BoxWidth]>0 AND [BoxHeight]>0 AND [BoxX]+[BoxWidth]<=1 AND [BoxY]+[BoxHeight]<=1");
                    table.CheckConstraint("CK_Anh02AiDetection_Time", "[TimestampMilliseconds]>=0 AND [SourceDurationMilliseconds]>0 AND [TimestampMilliseconds]<[SourceDurationMilliseconds]");
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_AIDetections_DetectionId",
                        column: x => x.DetectionId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_Files_FrameFileId",
                        column: x => x.FrameFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_Files_SourceVideoFileId",
                        column: x => x.SourceVideoFileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiDetectionProvenance_RoadSegments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02AiManifestFiles",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiManifestFiles", x => new { x.RunId, x.FileId });
                    table.UniqueConstraint("AK_Anh02AiManifestFiles_RunId_FileId_FileVersion", x => new { x.RunId, x.FileId, x.FileVersion });
                    table.ForeignKey(
                        name: "FK_Anh02AiManifestFiles_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02AiMockRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Stage = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    FixtureVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    GeometryVersion = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ManifestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CanonicalManifest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResultTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiMockRuns", x => x.Id);
                    table.CheckConstraint("CK_Anh02AiMockRuns_Completion", "([Status]='SUCCEEDED' AND [ResultId] IS NOT NULL AND [CompletedAt] IS NOT NULL) OR ([Status]<>'SUCCEEDED' AND [ResultId] IS NULL)");
                    table.CheckConstraint("CK_Anh02AiMockRuns_Manifest", "ISJSON([CanonicalManifest])=1");
                    table.CheckConstraint("CK_Anh02AiMockRuns_Stage", "[Stage] IN ('VIDEO_ANALYSIS','DUPLICATE_MATCHING')");
                    table.CheckConstraint("CK_Anh02AiMockRuns_Status", "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')");
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_AIModelVersions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "AIModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_Anh02AiMockRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "Anh02AiMockRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_ProcessingAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ProcessingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_RoadSegmentSets_SegmentSetId_RouteVersionId",
                        columns: x => new { x.SegmentSetId, x.RouteVersionId },
                        principalTable: "RoadSegmentSets",
                        principalColumns: new[] { "Id", "RoadSectionVersionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_SurveyDataVersions_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalTable: "SurveyDataVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiMockRuns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02AiResultProvenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessingJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManifestHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ResultHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CanonicalResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02AiResultProvenance", x => x.Id);
                    table.UniqueConstraint("AK_Anh02AiResultProvenance_Id_RunId", x => new { x.Id, x.RunId });
                    table.CheckConstraint("CK_Anh02AiResult_Json", "ISJSON([CanonicalResult])=1");
                    table.ForeignKey(
                        name: "FK_Anh02AiResultProvenance_Anh02AiMockRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "Anh02AiMockRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiResultProvenance_ProcessingAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ProcessingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02AiResultProvenance_ProcessingJobs_ProcessingJobId",
                        column: x => x.ProcessingJobId,
                        principalTable: "ProcessingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_FrameFileId",
                table: "Anh02AiDetectionProvenance",
                column: "FrameFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_ResultId_RunId",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "ResultId", "RunId" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_RunId_SourceVideoFileId_SourceVideoFileVersion",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "RunId", "SourceVideoFileId", "SourceVideoFileVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_SegmentId",
                table: "Anh02AiDetectionProvenance",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiDetectionProvenance_SourceVideoFileId",
                table: "Anh02AiDetectionProvenance",
                column: "SourceVideoFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiManifestFiles_FileId",
                table: "Anh02AiManifestFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_AnalysisRunId",
                table: "Anh02AiMockRuns",
                column: "AnalysisRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_AttemptId",
                table: "Anh02AiMockRuns",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_CreatedBy",
                table: "Anh02AiMockRuns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_DatasetVersionId",
                table: "Anh02AiMockRuns",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ModelVersionId",
                table: "Anh02AiMockRuns",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ProcessingJobId",
                table: "Anh02AiMockRuns",
                column: "ProcessingJobId",
                unique: true,
                filter: "[Stage]='VIDEO_ANALYSIS'");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ProjectId",
                table: "Anh02AiMockRuns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_ResultId_Id",
                table: "Anh02AiMockRuns",
                columns: new[] { "ResultId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_SegmentSetId_RouteVersionId",
                table: "Anh02AiMockRuns",
                columns: new[] { "SegmentSetId", "RouteVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiMockRuns_Status_LeaseUntil",
                table: "Anh02AiMockRuns",
                columns: new[] { "Status", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiResultProvenance_AttemptId",
                table: "Anh02AiResultProvenance",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiResultProvenance_ProcessingJobId",
                table: "Anh02AiResultProvenance",
                column: "ProcessingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02AiResultProvenance_RunId",
                table: "Anh02AiResultProvenance",
                column: "RunId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiDetectionProvenance_Anh02AiManifestFiles_RunId_SourceVideoFileId_SourceVideoFileVersion",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "RunId", "SourceVideoFileId", "SourceVideoFileVersion" },
                principalTable: "Anh02AiManifestFiles",
                principalColumns: new[] { "RunId", "FileId", "FileVersion" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiDetectionProvenance_Anh02AiMockRuns_RunId",
                table: "Anh02AiDetectionProvenance",
                column: "RunId",
                principalTable: "Anh02AiMockRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiDetectionProvenance_Anh02AiResultProvenance_ResultId_RunId",
                table: "Anh02AiDetectionProvenance",
                columns: new[] { "ResultId", "RunId" },
                principalTable: "Anh02AiResultProvenance",
                principalColumns: new[] { "Id", "RunId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiManifestFiles_Anh02AiMockRuns_RunId",
                table: "Anh02AiManifestFiles",
                column: "RunId",
                principalTable: "Anh02AiMockRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02AiMockRuns_Anh02AiResultProvenance_ResultId_Id",
                table: "Anh02AiMockRuns",
                columns: new[] { "ResultId", "Id" },
                principalTable: "Anh02AiResultProvenance",
                principalColumns: new[] { "Id", "RunId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("CREATE TRIGGER [TR_Anh02AiResultProvenance_Immutable] ON [dbo].[Anh02AiResultProvenance] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'AI result provenance is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_Anh02AiDetectionProvenance_Immutable] ON [dbo].[Anh02AiDetectionProvenance] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'AI detection provenance is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_Anh02AiManifestFiles_Immutable] ON [dbo].[Anh02AiManifestFiles] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'AI manifest file provenance is immutable.', 1; END");
            migrationBuilder.Sql(@"CREATE TRIGGER [TR_Anh02AiMockRuns_Identity] ON [dbo].[Anh02AiMockRuns] AFTER UPDATE, DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS (SELECT Id FROM deleted EXCEPT SELECT Id FROM inserted)
    THROW 51221, 'AI run identity cannot be deleted.', 1;
 IF EXISTS (SELECT Id, [ProjectId], [DatasetVersionId], [ProcessingJobId], [AttemptId], [ModelVersionId], [RouteVersionId], [SegmentSetId], [CreatedBy], [CreatedAt], CONVERT(varbinary(max), [Stage]), CONVERT(varbinary(max), [FixtureVersion]), CONVERT(varbinary(max), [GeometryVersion]), CONVERT(varbinary(max), [ManifestHash]), CONVERT(varbinary(max), [CanonicalManifest]), [AnalysisRunId] FROM deleted EXCEPT SELECT Id, [ProjectId], [DatasetVersionId], [ProcessingJobId], [AttemptId], [ModelVersionId], [RouteVersionId], [SegmentSetId], [CreatedBy], [CreatedAt], CONVERT(varbinary(max), [Stage]), CONVERT(varbinary(max), [FixtureVersion]), CONVERT(varbinary(max), [GeometryVersion]), CONVERT(varbinary(max), [ManifestHash]), CONVERT(varbinary(max), [CanonicalManifest]), [AnalysisRunId] FROM inserted)
    THROW 51221, 'AI source identity is immutable.', 1;
 IF EXISTS (SELECT 1 FROM deleted WHERE Status IN ('SUCCEEDED','FAILED'))
    THROW 51221, 'AI terminal result is immutable.', 1;
 IF EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON d.Id=i.Id
    WHERE (d.Status='QUEUED' AND i.Status NOT IN ('QUEUED','RUNNING','FAILED'))
       OR (d.Status='RUNNING' AND i.Status NOT IN ('RUNNING','SUCCEEDED','FAILED'))
       OR (d.ResultTimestamp IS NOT NULL AND (i.ResultTimestamp IS NULL OR i.ResultTimestamp<>d.ResultTimestamp)))
    THROW 51221, 'AI stage transition or result timestamp is invalid.', 1;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Anh02AiMockRuns]) OR EXISTS (SELECT 1 FROM [Anh02AiResultProvenance]) OR EXISTS (SELECT 1 FROM [Anh02AiDetectionProvenance]) OR EXISTS (SELECT 1 FROM [Anh02AiManifestFiles]) THROW 51000, 'AI producer downgrade requires an empty disposable graph.', 1;");
            migrationBuilder.Sql("DROP TRIGGER [TR_Anh02AiMockRuns_Identity]; DROP TRIGGER [TR_Anh02AiManifestFiles_Immutable]; DROP TRIGGER [TR_Anh02AiDetectionProvenance_Immutable]; DROP TRIGGER [TR_Anh02AiResultProvenance_Immutable];");
            migrationBuilder.DropForeignKey(
                name: "FK_Anh02AiResultProvenance_Anh02AiMockRuns_RunId",
                table: "Anh02AiResultProvenance");

            migrationBuilder.DropTable(
                name: "Anh02AiDetectionProvenance");

            migrationBuilder.DropTable(
                name: "Anh02AiManifestFiles");

            migrationBuilder.DropTable(
                name: "Anh02AiMockRuns");

            migrationBuilder.DropTable(
                name: "Anh02AiResultProvenance");
        }
    }
}
