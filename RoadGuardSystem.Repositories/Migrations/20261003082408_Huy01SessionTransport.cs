using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class Huy01SessionTransport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActivityAt",
                table: "Sessions",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Transport",
                table: "Sessions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sessions_Transport",
                table: "Sessions",
                sql: "[Transport] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Sessions_Transport",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Transport",
                table: "Sessions");
        }
    }
}
