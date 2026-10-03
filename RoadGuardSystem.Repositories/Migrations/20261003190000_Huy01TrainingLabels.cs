using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class Huy01TrainingLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AIDetectionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentRevision = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLabels", x => x.Id);
                    table.CheckConstraint("CK_TrainingLabels_Source", "([SourceKind]='REPORT' AND [ReportSourceId]=[SourceId] AND [AIDetectionSourceId] IS NULL) OR ([SourceKind]='AI_DETECTION' AND [AIDetectionSourceId]=[SourceId] AND [ReportSourceId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_TrainingLabels_AIDetections_AIDetectionSourceId",
                        column: x => x.AIDetectionSourceId,
                        principalTable: "AIDetections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabels_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabels_Reports_ReportSourceId",
                        column: x => x.ReportSourceId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLabelRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    X = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    Y = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(9,7)", precision: 9, scale: 7, nullable: false),
                    DefectTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLabelRevisions", x => x.Id);
                    table.UniqueConstraint("AK_TrainingLabelRevisions_Id_LabelId", x => new { x.Id, x.LabelId });
                    table.CheckConstraint("CK_TrainingLabelRevisions_Bbox", "[X]>=0 AND [Y]>=0 AND [Width]>0 AND [Height]>0 AND [X]+[Width]<=1 AND [Y]+[Height]<=1");
                    table.ForeignKey(
                        name: "FK_TrainingLabelRevisions_DefectTypes_DefectTypeCode",
                        column: x => x.DefectTypeCode,
                        principalTable: "DefectTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabelRevisions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabelRevisions_TrainingLabels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "TrainingLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingLabelReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLabelReviews", x => x.Id);
                    table.CheckConstraint("CK_TrainingLabelReviews_Decision", "[Decision] IN ('APPROVED','REJECTED')");
                    table.ForeignKey(
                        name: "FK_TrainingLabelReviews_TrainingLabelRevisions_RevisionId_LabelId",
                        columns: x => new { x.RevisionId, x.LabelId },
                        principalTable: "TrainingLabelRevisions",
                        principalColumns: new[] { "Id", "LabelId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingLabelReviews_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelReviews_ActorUserId",
                table: "TrainingLabelReviews",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelReviews_RevisionId",
                table: "TrainingLabelReviews",
                column: "RevisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelReviews_RevisionId_LabelId",
                table: "TrainingLabelReviews",
                columns: new[] { "RevisionId", "LabelId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelRevisions_DefectTypeCode",
                table: "TrainingLabelRevisions",
                column: "DefectTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelRevisions_FileId",
                table: "TrainingLabelRevisions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabelRevisions_LabelId_Revision",
                table: "TrainingLabelRevisions",
                columns: new[] { "LabelId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabels_AIDetectionSourceId",
                table: "TrainingLabels",
                column: "AIDetectionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabels_ProjectId_SourceKind_SourceId",
                table: "TrainingLabels",
                columns: new[] { "ProjectId", "SourceKind", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLabels_ReportSourceId",
                table: "TrainingLabels",
                column: "ReportSourceId");

            migrationBuilder.Sql("CREATE TRIGGER [TR_TrainingLabelRevisions_AppendOnly] ON [TrainingLabelRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51001, 'Training label revisions are append-only.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_TrainingLabelReviews_AppendOnly] ON [TrainingLabelReviews] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51001, 'Training label reviews are append-only.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_TrainingLabels_Identity] ON [TrainingLabels] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL OR i.ProjectId<>d.ProjectId OR i.SourceKind<>d.SourceKind OR i.SourceId<>d.SourceId OR ISNULL(i.ReportSourceId,'00000000-0000-0000-0000-000000000000')<>ISNULL(d.ReportSourceId,'00000000-0000-0000-0000-000000000000') OR ISNULL(i.AIDetectionSourceId,'00000000-0000-0000-0000-000000000000')<>ISNULL(d.AIDetectionSourceId,'00000000-0000-0000-0000-000000000000') OR i.CurrentRevision<d.CurrentRevision OR i.CurrentRevision>d.CurrentRevision+1) THROW 51001, 'Training label identity/current head is immutable.', 1; END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [TrainingLabels]) THROW 51000, 'Training label downgrade requires an empty disposable graph.', 1;");
            migrationBuilder.Sql("DROP TRIGGER [TR_TrainingLabels_Identity]; DROP TRIGGER [TR_TrainingLabelReviews_AppendOnly]; DROP TRIGGER [TR_TrainingLabelRevisions_AppendOnly];");
            migrationBuilder.DropTable(
                name: "TrainingLabelReviews");

            migrationBuilder.DropTable(
                name: "TrainingLabelRevisions");

            migrationBuilder.DropTable(
                name: "TrainingLabels");
        }
    }
}
