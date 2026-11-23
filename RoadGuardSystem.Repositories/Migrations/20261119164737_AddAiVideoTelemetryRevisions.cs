using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAiVideoTelemetryRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiVideoTelemetryRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    PreviousId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    SourceVideoHash = table.Column<string>(type: "char(64)", unicode: false, nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IntervalsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalSrtObjectKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OriginalSrtSha256 = table.Column<string>(type: "char(64)", unicode: false, nullable: true),
                    OriginalSrtBytes = table.Column<long>(type: "bigint", nullable: true),
                    OffsetMs = table.Column<long>(type: "bigint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKeyHash = table.Column<string>(type: "char(64)", unicode: false, nullable: false),
                    PayloadFingerprint = table.Column<string>(type: "char(64)", unicode: false, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiVideoTelemetryRevisions", x => x.Id);
                    table.CheckConstraint("CK_AiVideoTelemetryRevisions_Json", "ISJSON([IntervalsJson])=1");
                    table.CheckConstraint("CK_AiVideoTelemetryRevisions_Kind", "([Kind]='EXTERNAL_SRT' AND [OriginalSrtObjectKey] IS NOT NULL AND [OriginalSrtSha256] IS NOT NULL AND [OriginalSrtBytes] BETWEEN 1 AND 2097152) OR ([Kind]='OPERATOR_ESTIMATE' AND [OriginalSrtObjectKey] IS NULL AND [OriginalSrtSha256] IS NULL AND [OriginalSrtBytes] IS NULL AND [Note] IS NOT NULL AND [OffsetMs]=0)");
                    table.CheckConstraint("CK_AiVideoTelemetryRevisions_Number", "([Number]=1 AND [PreviousId] IS NULL) OR ([Number]>1 AND [PreviousId] IS NOT NULL)");
                    table.CheckConstraint("CK_AiVideoTelemetryRevisions_Offset", "[OffsetMs] BETWEEN -86400000 AND 86400000");
                    table.ForeignKey(
                        name: "FK_AiVideoTelemetryRevisions_AiVideoTelemetryRevisions_PreviousId",
                        column: x => x.PreviousId,
                        principalTable: "AiVideoTelemetryRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoTelemetryRevisions_AiVideoWorkItems_WorkId",
                        column: x => x.WorkId,
                        principalTable: "AiVideoWorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiVideoTelemetryRevisions_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoTelemetryRevisions_ActorId",
                table: "AiVideoTelemetryRevisions",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoTelemetryRevisions_PreviousId",
                table: "AiVideoTelemetryRevisions",
                column: "PreviousId");

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoTelemetryRevisions_WorkId_ActorId_IdempotencyKeyHash",
                table: "AiVideoTelemetryRevisions",
                columns: new[] { "WorkId", "ActorId", "IdempotencyKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiVideoTelemetryRevisions_WorkId_Number",
                table: "AiVideoTelemetryRevisions",
                columns: new[] { "WorkId", "Number" },
                unique: true);

            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AiVideoTelemetryRevisions_AppendOnly] ON [AiVideoTelemetryRevisions]
                INSTEAD OF UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'AI_TELEMETRY_REVISION_APPEND_ONLY', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiVideoTelemetryRevisions");
        }
    }
}
