using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H5AuthenticatedAttachedPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachedPayloadHash",
                table: "OfflineSyncBatches",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachedPayloadJson",
                table: "OfflineSyncBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OfflineSyncBatches_AttachedPayload",
                table: "OfflineSyncBatches",
                sql: "([AttachedPayloadJson] IS NULL AND [AttachedPayloadHash] IS NULL) OR ([AttachedPayloadJson] IS NOT NULL AND [AttachedPayloadHash] IS NOT NULL AND ISJSON([AttachedPayloadJson])=1 AND LEN([AttachedPayloadHash])=64 AND [AttachedPayloadHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%' AND DATALENGTH(CONVERT(varchar(max),[AttachedPayloadJson] COLLATE Latin1_General_100_BIN2_UTF8))<=16777216)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM OfflineSyncBatches WHERE AttachedPayloadJson IS NOT NULL OR AttachedPayloadHash IS NOT NULL) THROW 51491, 'Retained authenticated payload cannot be erased by downgrade.', 1;");
            migrationBuilder.DropCheckConstraint(
                name: "CK_OfflineSyncBatches_AttachedPayload",
                table: "OfflineSyncBatches");

            migrationBuilder.DropColumn(
                name: "AttachedPayloadHash",
                table: "OfflineSyncBatches");

            migrationBuilder.DropColumn(
                name: "AttachedPayloadJson",
                table: "OfflineSyncBatches");
        }
    }
}
