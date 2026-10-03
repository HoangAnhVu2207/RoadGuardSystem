using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261003160000_AnhHuyDependencyDefectConcurrency")]
public partial class AnhHuyDependencyDefectConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Defects",
            type: "rowversion",
            rowVersion: true,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Defects]) THROW 51000, 'Defect concurrency downgrade requires an empty disposable graph.', 1;");
        migrationBuilder.DropColumn(name: "RowVersion", table: "Defects");
    }
}
