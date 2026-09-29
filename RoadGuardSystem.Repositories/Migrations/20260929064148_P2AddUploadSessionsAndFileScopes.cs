using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class P2AddUploadSessionsAndFileScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileScopes_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileScopes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileScopes_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectKey = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    Purpose = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    MediaType = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    ExpectedSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedChecksumSha256 = table.Column<string>(type: "char(64)", nullable: false),
                    PartSizeBytes = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StorageUploadId = table.Column<string>(type: "varchar(1024)", unicode: false, maxLength: 1024, nullable: true),
                    FailureCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.Id);
                    table.CheckConstraint("CK_UploadSessions_ExpectedSizeBytes_Positive", "[ExpectedSizeBytes] > 0");
                    table.CheckConstraint("CK_UploadSessions_PartSizeBytes_Positive", "[PartSizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_UploadSessions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UploadSessions_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartNumber = table.Column<int>(type: "int", nullable: false),
                    ETag = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: true),
                    UrlIssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    UrlExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadParts_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileScopes_OwnerUserId",
                table: "FileScopes",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileScopes_ProjectId_OwnerUserId",
                table: "FileScopes",
                columns: new[] { "ProjectId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "UX_FileScopes_FileId",
                table: "FileScopes",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UploadParts_Session_PartNumber",
                table: "UploadParts",
                columns: new[] { "UploadSessionId", "PartNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_FileId",
                table: "UploadSessions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_OwnerUserId",
                table: "UploadSessions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_Status_ExpiresAt",
                table: "UploadSessions",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "UX_UploadSessions_ObjectKey",
                table: "UploadSessions",
                column: "ObjectKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileScopes");

            migrationBuilder.DropTable(
                name: "UploadParts");

            migrationBuilder.DropTable(
                name: "UploadSessions");
        }
    }
}
