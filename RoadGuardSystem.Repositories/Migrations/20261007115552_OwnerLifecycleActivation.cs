using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class OwnerLifecycleActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Defects_Status",
                table: "Defects");

            migrationBuilder.CreateTable(
                name: "LD06LifecycleActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedDefectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivingProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PriorRepairDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ScopeHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LD06LifecycleActions", x => x.Id);
                    table.CheckConstraint("CK_LD06LifecycleActions_Kind", "[Kind] BETWEEN 1 AND 7 AND ISJSON([FactsJson])=1");
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Defects_DefectId",
                        column: x => x.DefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Defects_LinkedDefectId",
                        column: x => x.LinkedDefectId,
                        principalTable: "Defects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_LD06LifecycleActions_SourceActionId",
                        column: x => x.SourceActionId,
                        principalTable: "LD06LifecycleActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Projects_ReceivingProjectId",
                        column: x => x.ReceivingProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_RepairDecisions_PriorRepairDecisionId",
                        column: x => x.PriorRepairDecisionId,
                        principalTable: "RepairDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06LifecycleActions_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LD06ActionEvidence",
                columns: table => new
                {
                    ActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Checksum = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LD06ActionEvidence", x => new { x.ActionId, x.FileId });
                    table.ForeignKey(
                        name: "FK_LD06ActionEvidence_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LD06ActionEvidence_LD06LifecycleActions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "LD06LifecycleActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObligationResponsibilities",
                columns: table => new
                {
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptanceActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObligationResponsibilities", x => x.ObligationId);
                    table.ForeignKey(
                        name: "FK_ObligationResponsibilities_LD06LifecycleActions_AcceptanceActionId",
                        column: x => x.AcceptanceActionId,
                        principalTable: "LD06LifecycleActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObligationResponsibilities_Projects_CurrentProjectId",
                        column: x => x.CurrentProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObligationResponsibilities_Projects_OriginProjectId",
                        column: x => x.OriginProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObligationResponsibilities_RepairObligations_ObligationId",
                        column: x => x.ObligationId,
                        principalTable: "RepairObligations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Defects_Status",
                table: "Defects",
                sql: "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_LD06ActionEvidence_FileId",
                table: "LD06ActionEvidence",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ActorId",
                table: "LD06LifecycleActions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_DefectId",
                table: "LD06LifecycleActions",
                column: "DefectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_LinkedDefectId",
                table: "LD06LifecycleActions",
                column: "LinkedDefectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ObligationId",
                table: "LD06LifecycleActions",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_PriorRepairDecisionId",
                table: "LD06LifecycleActions",
                column: "PriorRepairDecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ProjectId",
                table: "LD06LifecycleActions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_ReceivingProjectId",
                table: "LD06LifecycleActions",
                column: "ReceivingProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LD06LifecycleActions_SourceActionId_Kind",
                table: "LD06LifecycleActions",
                columns: new[] { "SourceActionId", "Kind" },
                unique: true,
                filter: "[SourceActionId] IS NOT NULL AND [Kind] IN (2,7)");

            migrationBuilder.CreateIndex(
                name: "IX_ObligationResponsibilities_AcceptanceActionId",
                table: "ObligationResponsibilities",
                column: "AcceptanceActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ObligationResponsibilities_CurrentProjectId",
                table: "ObligationResponsibilities",
                column: "CurrentProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ObligationResponsibilities_OriginProjectId",
                table: "ObligationResponsibilities",
                column: "OriginProjectId");
            InstallOwnerLifecycleGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS(SELECT 1 FROM LD06LifecycleActions)
                  OR EXISTS(SELECT 1 FROM ObligationResponsibilities)
                  OR EXISTS(SELECT 1 FROM LD06ActionEvidence)
                  OR EXISTS(SELECT 1 FROM Defects WHERE Status=5)
                  THROW 51690, 'Recorded owner lifecycle activation cannot be erased by downgrade.', 1;
                DROP TRIGGER TR_ProjectLifecycleHistory_Production;
                """);
            migrationBuilder.DropTable(
                name: "LD06ActionEvidence");

            migrationBuilder.DropTable(
                name: "ObligationResponsibilities");

            migrationBuilder.DropTable(
                name: "LD06LifecycleActions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Defects_Status",
                table: "Defects");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Defects_Status",
                table: "Defects",
                sql: "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 4)");
        }
    }
}
