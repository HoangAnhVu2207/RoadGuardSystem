using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityOnboardingEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReporterRegistrationIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    ReporterType = table.Column<byte>(type: "tinyint", nullable: false),
                    OtpHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    OtpGeneration = table.Column<int>(type: "int", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ResendAvailableAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    EmailConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReporterRegistrationIntents", x => x.Id);
                    table.CheckConstraint("CK_ReporterRegistrationIntents_ReporterType", "[ReporterType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_ReporterRegistrationIntents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    RoleCode = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffInvitations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffInvitationProjects",
                columns: table => new
                {
                    InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffInvitationProjects", x => new { x.InvitationId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_StaffInvitationProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffInvitationProjects_StaffInvitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "StaffInvitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReporterRegistrationIntents_Email",
                table: "ReporterRegistrationIntents",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ReporterRegistrationIntents_UserId",
                table: "ReporterRegistrationIntents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitationProjects_ProjectId",
                table: "StaffInvitationProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_CreatedByUserId",
                table: "StaffInvitations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInvitations_Email",
                table: "StaffInvitations",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UX_StaffInvitations_TokenHash",
                table: "StaffInvitations",
                column: "TokenHash",
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReporterRegistrationIntents");

            migrationBuilder.DropTable(
                name: "StaffInvitationProjects");

            migrationBuilder.DropTable(
                name: "StaffInvitations");

        }
    }
}
