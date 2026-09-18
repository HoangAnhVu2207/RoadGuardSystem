using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditOutboxIdempotencyConcurrencyPrimitives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EventType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeforeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.CheckConstraint("CK_AuditLogs_AfterSnapshot_Json", "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1");
                    table.CheckConstraint("CK_AuditLogs_BeforeSnapshot_Json", "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Operation = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutcomeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                    table.CheckConstraint("CK_IdempotencyRecords_OutcomeJson_Json", "ISJSON([OutcomeJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessages_PayloadJson_Json", "ISJSON([PayloadJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "ConsumerEffectReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EffectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumerEffectReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumerEffectReceipts_OutboxMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAtUtc",
                table: "AuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "UX_ConsumerEffectReceipts_MessageConsumer",
                table: "ConsumerEffectReceipts",
                columns: new[] { "MessageId", "ConsumerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_OperationId",
                table: "IdempotencyRecords",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecords_ScopeKey",
                table: "IdempotencyRecords",
                columns: new[] { "ActorUserId", "ProjectId", "Operation", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CorrelationId",
                table: "OutboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredAtUtc",
                table: "OutboxMessages",
                column: "OccurredAtUtc");

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_AuditLogs_AppendOnly]
                ON [AuditLogs]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, 'AuditLogs are append-only; corrections require a new audit event.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_AuditLogs_AppendOnly]");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ConsumerEffectReceipts");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
