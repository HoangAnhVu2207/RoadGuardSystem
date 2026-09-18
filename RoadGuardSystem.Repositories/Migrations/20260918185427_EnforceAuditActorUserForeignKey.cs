using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAuditActorUserForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1 FROM [AuditLogs] [a]
                    WHERE [a].[ActorUserId] IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM [Users] [u] WHERE [u].[Id] = [a].[ActorUserId])
                )
                BEGIN
                    THROW 51000, 'Migration precondition failed: AuditLogs contains legacy ActorUserId values that are not mapped to existing Users. Attributions must not be silently resolved. Apply the identity-schema stage, provision each verified User with its original actor ID, then retry this FK migration.', 1;
                END
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_ActorUserId",
                table: "AuditLogs");
        }
    }
}
