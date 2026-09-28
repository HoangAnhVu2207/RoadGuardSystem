using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReadVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OccurredAtUtc",
                table: "Notifications",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Notifications",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientOccurredAtId",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "OccurredAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientOccurredAtId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "OccurredAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Notifications");
        }
    }
}
