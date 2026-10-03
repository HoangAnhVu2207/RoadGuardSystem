using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class Anh01MultipartRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MultipartDeadline",
                table: "UploadSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MultipartFence",
                table: "UploadSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MultipartNextCheckAt",
                table: "UploadSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MultipartPhase",
                table: "UploadSessions",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UploadMultipartSweeps",
                columns: table => new
                {
                    UploadSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NextCheckAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadMultipartSweeps", x => x.UploadSessionId);
                    table.ForeignKey(
                        name: "FK_UploadMultipartSweeps_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_MultipartNextCheckAt",
                table: "UploadSessions",
                column: "MultipartNextCheckAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadMultipartSweeps_NextCheckAt",
                table: "UploadMultipartSweeps",
                column: "NextCheckAt");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM UploadSessions WHERE MultipartPhase IS NOT NULL) THROW 51000, 'Multipart recovery state must be preserved; populated downgrade is blocked.', 1;");

            migrationBuilder.DropTable(
                name: "UploadMultipartSweeps");

            migrationBuilder.DropIndex(
                name: "IX_UploadSessions_MultipartNextCheckAt",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "MultipartDeadline",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "MultipartFence",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "MultipartNextCheckAt",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "MultipartPhase",
                table: "UploadSessions");

        }
    }
}
