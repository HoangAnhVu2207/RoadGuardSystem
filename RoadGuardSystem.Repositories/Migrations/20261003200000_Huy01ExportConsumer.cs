using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class Huy01ExportConsumer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anh02ExportSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hash = table.Column<string>(type: "char(64)", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02ExportSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anh02ExportSnapshots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02ExportSnapshotFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Included = table.Column<bool>(type: "bit", nullable: false),
                    ArchivePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02ExportSnapshotFiles", x => x.Id);
                    table.CheckConstraint("CK_Anh02ExportSnapshotFile_Size", "[SizeBytes]>0");
                    table.ForeignKey(
                        name: "FK_Anh02ExportSnapshotFiles_Anh02ExportSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Anh02ExportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02ExportSnapshotFiles_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Format = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02ExportJobs", x => x.Id);
                    table.CheckConstraint("CK_Anh02ExportJob_Expiry", "([Status] <> 'SUCCEEDED' AND [ExpiresAt] IS NULL) OR ([Status]='SUCCEEDED' AND [ArtifactId] IS NOT NULL AND [CompletedAt] IS NOT NULL AND [ExpiresAt]=DATEADD(day,30,[CompletedAt]))");
                    table.CheckConstraint("CK_Anh02ExportJob_Format", "[Format] IN ('PDF','ZIP') AND ([Kind] <> 'TRAINING' OR [Format]='ZIP')");
                    table.CheckConstraint("CK_Anh02ExportJob_Kind", "[Kind] IN ('DOSSIER','TRAINING')");
                    table.CheckConstraint("CK_Anh02ExportJob_State", "[Status] IN ('QUEUED','RUNNING','SUCCEEDED','FAILED')");
                    table.ForeignKey(
                        name: "FK_Anh02ExportJobs_Anh02ExportSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Anh02ExportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02ExportJobs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02ExportJobs_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anh02GeneratedArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anh02GeneratedArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anh02GeneratedArtifacts_Anh02ExportJobs_ExportJobId",
                        column: x => x.ExportJobId,
                        principalTable: "Anh02ExportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02GeneratedArtifacts_Anh02ExportSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Anh02ExportSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anh02GeneratedArtifacts_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_ArtifactId",
                table: "Anh02ExportJobs",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_ProjectId",
                table: "Anh02ExportJobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_RequestedBy",
                table: "Anh02ExportJobs",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_SnapshotId",
                table: "Anh02ExportJobs",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportJobs_Status_NextAttemptAt_LeaseUntil",
                table: "Anh02ExportJobs",
                columns: new[] { "Status", "NextAttemptAt", "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportSnapshotFiles_FileId",
                table: "Anh02ExportSnapshotFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportSnapshotFiles_SnapshotId_FileId",
                table: "Anh02ExportSnapshotFiles",
                columns: new[] { "SnapshotId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02ExportSnapshots_ProjectId",
                table: "Anh02ExportSnapshots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Anh02GeneratedArtifacts_ExportJobId",
                table: "Anh02GeneratedArtifacts",
                column: "ExportJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02GeneratedArtifacts_FileId",
                table: "Anh02GeneratedArtifacts",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anh02GeneratedArtifacts_SnapshotId",
                table: "Anh02GeneratedArtifacts",
                column: "SnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Anh02ExportJobs_Anh02GeneratedArtifacts_ArtifactId",
                table: "Anh02ExportJobs",
                column: "ArtifactId",
                principalTable: "Anh02GeneratedArtifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("CREATE TRIGGER [TR_Anh02ExportSnapshots_Immutable] ON [dbo].[Anh02ExportSnapshots] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Export snapshot is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_Anh02ExportSnapshotFiles_Immutable] ON [dbo].[Anh02ExportSnapshotFiles] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Export snapshot file is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_Anh02GeneratedArtifacts_Immutable] ON [dbo].[Anh02GeneratedArtifacts] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Generated artifact is immutable.', 1; END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Anh02ExportJobs]) OR EXISTS (SELECT 1 FROM [Anh02ExportSnapshots]) THROW 51000, 'Export consumer downgrade requires an empty disposable graph.', 1;");
            migrationBuilder.Sql("DROP TRIGGER [TR_Anh02GeneratedArtifacts_Immutable]; DROP TRIGGER [TR_Anh02ExportSnapshotFiles_Immutable]; DROP TRIGGER [TR_Anh02ExportSnapshots_Immutable];");
            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Anh02ExportSnapshots_SnapshotId",
                table: "Anh02ExportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02GeneratedArtifacts_Anh02ExportSnapshots_SnapshotId",
                table: "Anh02GeneratedArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_Anh02ExportJobs_Anh02GeneratedArtifacts_ArtifactId",
                table: "Anh02ExportJobs");

            migrationBuilder.DropTable(
                name: "Anh02ExportSnapshotFiles");

            migrationBuilder.DropTable(
                name: "Anh02ExportSnapshots");

            migrationBuilder.DropTable(
                name: "Anh02GeneratedArtifacts");

            migrationBuilder.DropTable(
                name: "Anh02ExportJobs");
        }
    }
}
