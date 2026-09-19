using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableFileStorageBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    SizeBytes = table.Column<int>(type: "int", nullable: false),
                    Checksum = table.Column<string>(type: "char(64)", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RetentionUntil = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.CheckConstraint("CK_Files_Checksum_Sha256Lowercase", "LEN([Checksum]) = 64 AND [Checksum] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9a-f]%'");
                    table.CheckConstraint("CK_Files_SizeBytes_NonNegative", "[SizeBytes] >= 0");
                    table.ForeignKey(
                        name: "FK_Files_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_UploadedByUserId",
                table: "Files",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_Files_StorageUri",
                table: "Files",
                column: "StorageUri",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_Files_Immutable]
                ON [Files]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51020, 'Files are immutable; create a new file identity and use the retention workflow for deletion.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}
