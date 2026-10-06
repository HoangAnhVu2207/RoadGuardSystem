using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H6ProjectLifecycleHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectLifecycleHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperationalClosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedDefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BasisReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AuthoritySourceReference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceDisposition = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectLifecycleHistory", x => x.Id);
                    table.UniqueConstraint("AK_ProjectLifecycleHistory_Id_ProjectId", x => new { x.Id, x.ProjectId });
                    table.CheckConstraint("CK_ProjectLifecycleHistory_Facts", "ISJSON([FactsJson])=1");
                    table.CheckConstraint("CK_ProjectLifecycleHistory_Source", "[Kind] IN (1,2,3,4,5,6,7,8) AND [SourceDisposition] IN ('CANDIDATE','TARGET_CONFIRMED') AND ([SourceDisposition]='CANDIDATE' OR LEN([AuthoritySourceReference])>0)");
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Defects_LinkedDefectId",
                        column: x => x.LinkedDefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_ProjectLifecycleHistory_OperationalClosureId_ProjectId",
                        columns: x => new { x.OperationalClosureId, x.ProjectId },
                        principalTable: "ProjectLifecycleHistory",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectLifecycleHistory_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ActorId",
                table: "ProjectLifecycleHistory",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_DefectId",
                table: "ProjectLifecycleHistory",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_LinkedDefectId",
                table: "ProjectLifecycleHistory",
                column: "LinkedDefectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ObligationId",
                table: "ProjectLifecycleHistory",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_OperationalClosureId_ProjectId",
                table: "ProjectLifecycleHistory",
                columns: new[] { "OperationalClosureId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ProjectId",
                table: "ProjectLifecycleHistory",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectLifecycleHistory_ReceiverId",
                table: "ProjectLifecycleHistory",
                column: "ReceiverId");
            InstallLifecycleHistoryGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM ProjectLifecycleHistory) THROW 51590, 'Lifecycle source history requires a data-preserving migration.', 1;");
            migrationBuilder.DropTable(
                name: "ProjectLifecycleHistory");
        }
    }
}
