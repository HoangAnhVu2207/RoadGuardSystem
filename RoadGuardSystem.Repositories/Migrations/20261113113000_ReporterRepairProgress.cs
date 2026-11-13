using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;

namespace RoadGuardSystem.cRepositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20261113113000_ReporterRepairProgress")]
public sealed class ReporterRepairProgress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RepairAcceptanceFingerprint", table: "CasePublicationRecipients", type: "varchar(64)",
            unicode: false, maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "RepairProgressStatus", table: "CasePublicationRecipients", type: "varchar(16)",
            unicode: false, maxLength: 16, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RepairAcceptanceFingerprint", table: "CasePublicationRecipients");
        migrationBuilder.DropColumn(name: "RepairProgressStatus", table: "CasePublicationRecipients");
    }
}
