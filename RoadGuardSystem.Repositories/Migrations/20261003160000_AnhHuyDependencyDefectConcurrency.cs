using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AnhHuyDependencyDefectConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Defects",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Defects]) THROW 51000, 'Defect concurrency downgrade requires an empty disposable graph.', 1;");
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Defects");
        }
    }
}
