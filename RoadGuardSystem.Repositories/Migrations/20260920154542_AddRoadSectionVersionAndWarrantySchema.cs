using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadSectionVersionAndWarrantySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EngineeringUtmSrid",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoadSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadSections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadSectionVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Geometry = table.Column<LineString>(type: "geometry", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSectionVersions", x => x.Id);
                    table.CheckConstraint("CK_RoadSectionVersions_Geometry_AllowedSrid", "[Geometry].STSrid IN (32648, 32649)");
                    table.CheckConstraint("CK_RoadSectionVersions_Geometry_LineString", "[Geometry].STGeometryType() = 'LineString'");
                    table.CheckConstraint("CK_RoadSectionVersions_VersionNo_Positive", "[VersionNo] > 0");
                    table.ForeignKey(
                        name: "FK_RoadSectionVersions_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warranties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoadSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandoverDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandoverDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WarrantyStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WarrantyEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RetainedValue = table.Column<decimal>(type: "decimal(19,2)", precision: 19, scale: 2, nullable: true),
                    Scope = table.Column<byte>(type: "tinyint", nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warranties", x => x.Id);
                    table.CheckConstraint("CK_Warranties_DateRange", "[WarrantyEndDate] >= [WarrantyStartDate]");
                    table.CheckConstraint("CK_Warranties_RetainedValue_NonNegative", "[RetainedValue] IS NULL OR [RetainedValue] >= 0");
                    table.CheckConstraint("CK_Warranties_Scope", "[Scope] IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_Warranties_ScopeRoadSection", "([Scope] <> 1 OR [RoadSectionId] IS NULL) AND ([Scope] <> 2 OR [RoadSectionId] IS NOT NULL)");
                    table.CheckConstraint("CK_Warranties_Status", "[Status] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_Warranties_Files_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warranties_HandoverDocuments_HandoverDocumentId",
                        column: x => x.HandoverDocumentId,
                        principalTable: "HandoverDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warranties_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Warranties_RoadSections_RoadSectionId",
                        column: x => x.RoadSectionId,
                        principalTable: "RoadSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projects_EngineeringUtmSrid",
                table: "Projects",
                sql: "[EngineeringUtmSrid] IS NULL OR [EngineeringUtmSrid] IN (32648, 32649)");

            migrationBuilder.CreateIndex(
                name: "UX_RoadSections_ProjectId_Code",
                table: "RoadSections",
                columns: new[] { "ProjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RoadSectionVersions_Current",
                table: "RoadSectionVersions",
                column: "RoadSectionId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_RoadSectionVersions_RoadSectionId_VersionNo",
                table: "RoadSectionVersions",
                columns: new[] { "RoadSectionId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_HandoverDocumentId",
                table: "Warranties",
                column: "HandoverDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_ProjectId",
                table: "Warranties",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_RoadSectionId",
                table: "Warranties",
                column: "RoadSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_SourceDocumentId",
                table: "Warranties",
                column: "SourceDocumentId");

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [dbo].[TR_RoadSectionVersions_Immutable]
                ON [dbo].[RoadSectionVersions]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF NOT EXISTS (SELECT 1 FROM inserted)
                    BEGIN
                        THROW 50000, 'RoadSectionVersion records cannot be deleted.', 1;
                    END;

                    IF UPDATE([RoadSectionId]) OR UPDATE([VersionNo]) OR UPDATE([Geometry]) OR
                       UPDATE([EffectiveFrom]) OR UPDATE([ChangeReason])
                    BEGIN
                        THROW 50001, 'RoadSectionVersion history is immutable; only IsCurrent may change.', 1;
                    END;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER [dbo].[TR_RoadSectionVersions_Immutable];");

            migrationBuilder.DropTable(
                name: "RoadSectionVersions");

            migrationBuilder.DropTable(
                name: "Warranties");

            migrationBuilder.DropTable(
                name: "RoadSections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Projects_EngineeringUtmSrid",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EngineeringUtmSrid",
                table: "Projects");
        }
    }
}
