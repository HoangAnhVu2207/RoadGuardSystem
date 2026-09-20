using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddDefectCatalogConsumerBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Defects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefectTypeCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    CauseCategoryCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Defects_CauseCategories_CauseCategoryCode",
                        column: x => x.CauseCategoryCode,
                        principalTable: "CauseCategories",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Defects_DefectTypes_DefectTypeCode",
                        column: x => x.DefectTypeCode,
                        principalTable: "DefectTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Defects_CauseCategoryCode",
                table: "Defects",
                column: "CauseCategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Defects_DefectTypeCode",
                table: "Defects",
                column: "DefectTypeCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Defects");
        }
    }
}
