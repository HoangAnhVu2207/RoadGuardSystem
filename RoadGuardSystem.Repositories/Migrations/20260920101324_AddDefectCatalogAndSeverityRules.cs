using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddDefectCatalogAndSeverityRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CauseCategories",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauseCategories", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "DefectTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefectTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SeverityRuleVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    RoadTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    RuleDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeverityRuleVersions", x => x.Id);
                    table.CheckConstraint("CK_SeverityRuleVersions_EffectiveDateRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_SeverityRuleVersions_RuleDefinition_JsonObject", "ISJSON([RuleDefinition]) = 1 AND LEFT(LTRIM([RuleDefinition]), 1) = '{'");
                    table.CheckConstraint("CK_SeverityRuleVersions_VersionNo_Positive", "[VersionNo] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "UX_SeverityRuleVersions_ScopeVersion",
                table: "SeverityRuleVersions",
                columns: new[] { "StandardCode", "RoadTypeCode", "VersionNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CauseCategories");

            migrationBuilder.DropTable(
                name: "DefectTypes");

            migrationBuilder.DropTable(
                name: "SeverityRuleVersions");
        }
    }
}
