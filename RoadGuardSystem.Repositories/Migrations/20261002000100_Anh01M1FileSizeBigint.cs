using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.Repositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261002000100_Anh01M1FileSizeBigint")]
public sealed class Anh01M1FileSizeBigint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_Files_SizeBytes_NonNegative", table: "Files");
        migrationBuilder.AlterColumn<long>(name: "SizeBytes", table: "Files", type: "bigint", nullable: false, oldClrType: typeof(int), oldType: "int");
        migrationBuilder.AddCheckConstraint(name: "CK_Files_SizeBytes_NonNegative", table: "Files", sql: "[SizeBytes] >= 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Files] WHERE [SizeBytes] > 2147483647) THROW 51021, 'Cannot narrow Files.SizeBytes while large files exist.', 1;");
        migrationBuilder.DropCheckConstraint(name: "CK_Files_SizeBytes_NonNegative", table: "Files");
        migrationBuilder.AlterColumn<int>(name: "SizeBytes", table: "Files", type: "int", nullable: false, oldClrType: typeof(long), oldType: "bigint");
        migrationBuilder.AddCheckConstraint(name: "CK_Files_SizeBytes_NonNegative", table: "Files", sql: "[SizeBytes] >= 0");
    }
}
