using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H0RetentionIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetentionBasisRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    InventoryVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InventoryComplete = table.Column<bool>(type: "bit", nullable: false),
                    WarrantyReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionBasisRevisions", x => x.Id);
                    table.UniqueConstraint("AK_RetentionBasisRevisions_Id_FileId", x => new { x.Id, x.FileId });
                    table.CheckConstraint("CK_RetentionBasis_Revision", "[Revision]>0 AND [PolicyVersion]='pr41a.v1' AND [Classification] IN ('EVIDENCE','TEMPORARY_EXPORT') AND ISJSON([WarrantyReferencesJson])=1");
                    table.ForeignKey(
                        name: "FK_RetentionBasisRevisions_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionBasisRevisions_RetentionBasisRevisions_SupersedesId_FileId",
                        columns: x => new { x.SupersedesId, x.FileId },
                        principalTable: "RetentionBasisRevisions",
                        principalColumns: new[] { "Id", "FileId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionBasisRevisions_Users_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SelectionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionEvaluations", x => x.Id);
                    table.CheckConstraint("CK_RetentionEvaluation_State", "[Status] IN ('QUEUED','COMPLETE') AND (([Status]='QUEUED' AND [EvaluatedAt] IS NULL) OR ([Status]='COMPLETE' AND [EvaluatedAt] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_RetentionEvaluations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionEvaluations_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReleasedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionHolds", x => x.Id);
                    table.CheckConstraint("CK_RetentionHold_State", "[ScopeType] IN ('PROJECT','FILE') AND [State] IN ('ACTIVE','RELEASED') AND (([State]='ACTIVE' AND [ReleasedBy] IS NULL AND [ReleasedAt] IS NULL) OR ([State]='RELEASED' AND [ReleasedBy] IS NOT NULL AND [ReleasedAt] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_RetentionHolds_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionHolds_Users_ReleasedBy",
                        column: x => x.ReleasedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionBasisHeads",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionBasisHeads", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_RetentionBasisHeads_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionBasisHeads_RetentionBasisRevisions_RevisionId_FileId",
                        columns: x => new { x.RevisionId, x.FileId },
                        principalTable: "RetentionBasisRevisions",
                        principalColumns: new[] { "Id", "FileId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionEvaluationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eligibility = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReasonCodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibleAfter = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BasisVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InventoryVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HoldVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ControlSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionEvaluationItems", x => x.Id);
                    table.CheckConstraint("CK_RetentionEvaluationItem_State", "[Eligibility] IN ('BLOCKED_HOLD','WAITING_RETENTION_BASIS','RETAIN_UNTIL','ELIGIBLE_FOR_REVIEW')");
                    table.ForeignKey(
                        name: "FK_RetentionEvaluationItems_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionEvaluationItems_RetentionEvaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "RetentionEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetentionHoldHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionHoldHistories", x => x.Id);
                    table.CheckConstraint("CK_RetentionHoldHistory_State", "[State] IN ('ACTIVE','RELEASED')");
                    table.ForeignKey(
                        name: "FK_RetentionHoldHistories_RetentionHolds_HoldId",
                        column: x => x.HoldId,
                        principalTable: "RetentionHolds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetentionHoldHistories_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisHeads_RevisionId_FileId",
                table: "RetentionBasisHeads",
                columns: new[] { "RevisionId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisRevisions_ConfirmedBy",
                table: "RetentionBasisRevisions",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisRevisions_FileId_Revision",
                table: "RetentionBasisRevisions",
                columns: new[] { "FileId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetentionBasisRevisions_SupersedesId_FileId",
                table: "RetentionBasisRevisions",
                columns: new[] { "SupersedesId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluationItems_EvaluationId_FileId",
                table: "RetentionEvaluationItems",
                columns: new[] { "EvaluationId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluationItems_FileId",
                table: "RetentionEvaluationItems",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluations_ProjectId",
                table: "RetentionEvaluations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluations_RequestedBy",
                table: "RetentionEvaluations",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionEvaluations_Status_CreatedAt",
                table: "RetentionEvaluations",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHoldHistories_ActorId",
                table: "RetentionHoldHistories",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHoldHistories_HoldId_OccurredAt",
                table: "RetentionHoldHistories",
                columns: new[] { "HoldId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHolds_CreatedBy",
                table: "RetentionHolds",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHolds_ReleasedBy",
                table: "RetentionHolds",
                column: "ReleasedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionHolds_ScopeType_ScopeId_State",
                table: "RetentionHolds",
                columns: new[] { "ScopeType", "ScopeId", "State" });

            migrationBuilder.Sql("CREATE TRIGGER [TR_RetentionBasisRevisions_Immutable] ON [dbo].[RetentionBasisRevisions] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Retention basis revisions are immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_RetentionHoldHistories_Immutable] ON [dbo].[RetentionHoldHistories] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Retention hold history is immutable.', 1; END");
            migrationBuilder.Sql("CREATE TRIGGER [TR_RetentionEvaluationItems_Immutable] ON [dbo].[RetentionEvaluationItems] AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51220, 'Retention evaluation snapshots are immutable.', 1; END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [RetentionBasisHeads]) OR EXISTS (SELECT 1 FROM [RetentionBasisRevisions]) OR EXISTS (SELECT 1 FROM [RetentionHolds]) OR EXISTS (SELECT 1 FROM [RetentionHoldHistories]) OR EXISTS (SELECT 1 FROM [RetentionEvaluations]) OR EXISTS (SELECT 1 FROM [RetentionEvaluationItems]) THROW 51000, 'Cannot downgrade populated retention integration; preserve control history.', 1;");
            migrationBuilder.DropTable(
                name: "RetentionBasisHeads");

            migrationBuilder.DropTable(
                name: "RetentionEvaluationItems");

            migrationBuilder.DropTable(
                name: "RetentionHoldHistories");

            migrationBuilder.DropTable(
                name: "RetentionBasisRevisions");

            migrationBuilder.DropTable(
                name: "RetentionEvaluations");

            migrationBuilder.DropTable(
                name: "RetentionHolds");
        }
    }
}
