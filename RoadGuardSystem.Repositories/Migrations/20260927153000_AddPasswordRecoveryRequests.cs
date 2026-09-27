using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations;

[DbContext(typeof(RoadGuardDbContext))]
[Migration("20260927153000_AddPasswordRecoveryRequests")]
public sealed class AddPasswordRecoveryRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PasswordRecoveryRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordRecoveryRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasswordRecoveryRequests_Users_TargetUserId",
                    column: x => x.TargetUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PasswordRecoveryRequests_TargetUserId_RequestedAtUtc",
            table: "PasswordRecoveryRequests",
            columns: new[] { "TargetUserId", "RequestedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "PasswordRecoveryRequests");
}
